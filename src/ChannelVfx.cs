using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Homeward
{
    /// <summary>
    /// Our own channeling visual: a rune ring on the ground under the player that
    /// fades in and slowly rotates, with soft motes rising from its edge. Built at
    /// runtime from two PNGs embedded in the DLL — no asset bundle, no Unity Editor.
    /// Local-only for now: other players don't see it (no ZNetView).
    /// </summary>
    internal static class ChannelVfx
    {
        private static Texture2D _ringTex;
        private static Texture2D _moteTex;
        private static Shader _shader;

        /// <summary>Build a ring at a world position. Returns null if it fails (logged).</summary>
        public static GameObject Build(Vector3 pos, Color color, float radius, bool visuals, bool sound, float ttlSeconds)
        {
            if (!visuals && !sound)
            {
                return null;
            }
            GameObject root = new GameObject("Homeward_ChannelVfx");
            try
            {
                root.transform.position = pos + new Vector3(0f, 0.06f, 0f);
                ChannelVfxBehaviour fx = root.AddComponent<ChannelVfxBehaviour>();
                fx.Init(visuals ? NewMaterial(RingTexture()) : null,
                        visuals ? NewMaterial(MoteTexture()) : null,
                        color, radius, ttlSeconds);
                if (sound)
                {
                    fx.Sound = ChannelSound.Attach(root, HomewardPlugin.ChannelSoundVolume.Value);
                }
                return root;
            }
            catch (Exception e)
            {
                HomewardPlugin.Log.LogError($"Channel VFX failed: {e}");
                UnityEngine.Object.Destroy(root);
                return null;
            }
        }

        /// <summary>Stop the sound, stop emitting, fade the ring out, then destroy.</summary>
        public static void FadeOut(GameObject root)
        {
            if (root == null)
            {
                return;
            }
            ChannelVfxBehaviour fx = root.GetComponent<ChannelVfxBehaviour>();
            if (fx != null)
            {
                fx.BeginFadeOut();
            }
            else
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static Color ParseColor(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color c) ? c : new Color(0.5f, 0.85f, 1f, 1f);
        }

        private static Material NewMaterial(Texture2D tex)
        {
            Material m = new Material(FindShader());
            m.mainTexture = tex;
            m.renderQueue = 3100; // transparent, drawn after the world
            return m;
        }

        /// <summary>
        /// Shaders get stripped from builds if nothing references them, so try a few
        /// common transparent ones and fall back to borrowing whatever a vanilla
        /// particle uses. Logged so we know which one we ended up with.
        /// </summary>
        private static Shader FindShader()
        {
            if (_shader != null)
            {
                return _shader;
            }
            string[] names =
            {
                "Legacy Shaders/Particles/Additive",
                "Legacy Shaders/Particles/Alpha Blended",
                "Sprites/Default",
                "Unlit/Transparent",
                "UI/Default",
            };
            foreach (string n in names)
            {
                Shader s = Shader.Find(n);
                if (s != null)
                {
                    HomewardPlugin.Log.LogInfo($"Channel VFX shader: {n}");
                    return _shader = s;
                }
            }
            Shader borrowed = BorrowParticleShader();
            if (borrowed != null)
            {
                HomewardPlugin.Log.LogInfo($"Channel VFX shader (borrowed): {borrowed.name}");
                return _shader = borrowed;
            }
            throw new InvalidOperationException("no usable shader found");
        }

        private static Shader BorrowParticleShader()
        {
            if (ObjectDB.instance == null)
            {
                return null;
            }
            foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
            {
                foreach (EffectList.EffectData fx in se.m_startEffects.m_effectPrefabs)
                {
                    if (fx.m_prefab == null) continue;
                    ParticleSystemRenderer r = fx.m_prefab.GetComponentInChildren<ParticleSystemRenderer>(true);
                    if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null)
                    {
                        return r.sharedMaterial.shader;
                    }
                }
            }
            return null;
        }

        private static Texture2D RingTexture() => _ringTex ?? (_ringTex = LoadEmbedded("Homeward.Resources.rune_ring.png"));
        private static Texture2D MoteTexture() => _moteTex ?? (_moteTex = LoadEmbedded("Homeward.Resources.mote.png"));

        private static Texture2D LoadEmbedded(string resourceName)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (s == null)
                {
                    throw new FileNotFoundException($"embedded resource missing: {resourceName}");
                }
                byte[] bytes = new byte[s.Length];
                s.Read(bytes, 0, bytes.Length);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
                ImageConversion.LoadImage(tex, bytes);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                return tex;
            }
        }
    }

    internal class ChannelVfxBehaviour : MonoBehaviour
    {
        private const float FadeInSeconds = 0.7f;
        private const float SpinDegreesPerSecond = 18f;

        private const float FadeOutSeconds = 0.5f;

        private Transform _ring;
        private Material _ringMat;
        private ParticleSystem _motes;
        private Color _color;
        private float _t0;
        private float _ttl;
        private float _fadeOutStart = -1f;

        public AudioSource Sound;

        public void Init(Material ringMat, Material moteMat, Color color, float radius, float ttlSeconds)
        {
            _color = color;
            _t0 = Time.time;
            _ttl = ttlSeconds;
            if (ringMat == null)
            {
                return; // sound-only
            }

            // Ground ring: a quad lying flat, spun around its normal.
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ring";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, worldPositionStays: false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (radius * 2f);
            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = ringMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _ring = quad.transform;
            _ringMat = ringMat;
            ApplyRingAlpha(0f);

            // Motes: emitted from a circle in the ground plane, drifting upward.
            GameObject motes = new GameObject("motes");
            motes.transform.SetParent(transform, worldPositionStays: false);
            _motes = motes.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = _motes.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = color;
            main.maxParticles = 300;
            main.gravityModifier = -0.06f; // gentle upward drift even before velocity kicks in

            ParticleSystem.EmissionModule emission = _motes.emission;
            emission.rateOverTime = 45f;

            ParticleSystem.ShapeModule shape = _motes.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.9f;
            shape.radiusThickness = 0.2f;   // emit near the rim
            shape.rotation = new Vector3(90f, 0f, 0f); // circle in the XZ (ground) plane

            // Unity requires x/y/z here to share one MinMaxCurve mode; mixing a
            // constant x/z with a two-constant y makes the module silently no-op.
            ParticleSystem.VelocityOverLifetimeModule vel = _motes.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.ColorOverLifetimeModule col = _motes.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            ParticleSystemRenderer pr = motes.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.sharedMaterial = moteMat;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;

            _motes.Play();
        }

        public void BeginFadeOut()
        {
            if (_fadeOutStart >= 0f)
            {
                return;
            }
            _fadeOutStart = Time.time;
            if (Sound != null)
            {
                Sound.Stop();
            }
            if (_motes != null)
            {
                _motes.Stop(true, ParticleSystemStopBehavior.StopEmitting); // let live motes finish
            }
            if (_ring == null)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (_ttl > 0f && _fadeOutStart < 0f && Time.time - _t0 > _ttl)
            {
                BeginFadeOut();
            }
            if (_ring == null)
            {
                return;
            }
            _ring.Rotate(0f, 0f, SpinDegreesPerSecond * Time.deltaTime, Space.Self);
            float alpha = Mathf.Clamp01((Time.time - _t0) / FadeInSeconds);
            if (_fadeOutStart >= 0f)
            {
                float k = (Time.time - _fadeOutStart) / FadeOutSeconds;
                alpha *= Mathf.Clamp01(1f - k);
                if (k >= 1f && (_motes == null || _motes.particleCount == 0))
                {
                    Destroy(gameObject);
                    return;
                }
            }
            ApplyRingAlpha(alpha);
        }

        private void ApplyRingAlpha(float a)
        {
            if (_ringMat == null)
            {
                return;
            }
            Color c = new Color(_color.r, _color.g, _color.b, _color.a * a);
            if (_ringMat.HasProperty("_TintColor"))
            {
                _ringMat.SetColor("_TintColor", c);
            }
            if (_ringMat.HasProperty("_Color"))
            {
                _ringMat.color = c;
            }
        }

        private void OnDestroy()
        {
            if (_ringMat != null)
            {
                Destroy(_ringMat);
            }
        }
    }
}
