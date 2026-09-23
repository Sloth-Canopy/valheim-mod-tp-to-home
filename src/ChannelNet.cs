using System.Collections.Generic;
using UnityEngine;

namespace Homeward
{
    /// <summary>
    /// Tells every client (including ourselves) when a player starts or stops
    /// channeling, so each builds the ring locally. Routed RPCs to Everybody are
    /// delivered locally as well, so the local player's ring comes through the
    /// same path; the sound is attached only when the channeler is us.
    /// Clients without the mod ignore unknown RPC names.
    /// </summary>
    internal static class ChannelNet
    {
        private const string RpcStart = "Homeward_ChannelStart";
        private const string RpcStop = "Homeward_ChannelStop";

        // Safety net: a ring whose Stop never arrives (sender crashed/disconnected) removes itself.
        private const float RingTtlSeconds = 30f;

        // --- Limits on what we accept from the network (see docs/security-audit.md) ---
        private const int MaxOtherRings = 12;          // rings from other players drawn at once
        private const float MaxStyleLength = 32;       // "#RRGGBBAA|1.23" is 14 chars
        private const float MaxDrawDistance = 200f;    // beyond this you couldn't see it anyway
        private const float MaxPosDrift = 4f;          // claimed position must be near the player's real one
        private const float MinStartInterval = 0.5f;   // per-sender rate limit, seconds

        private static ZRoutedRpc _registeredOn;
        private static readonly Dictionary<ZDOID, GameObject> _rings = new Dictionary<ZDOID, GameObject>();
        private static readonly Dictionary<long, float> _lastStartBySender = new Dictionary<long, float>();
        private static bool _localActive;

        /// <summary>ZRoutedRpc is recreated per world session; (re)register when it changes.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || rpc == _registeredOn)
            {
                return;
            }
            rpc.Register<ZDOID, Vector3, string>(RpcStart, OnStart);
            rpc.Register<ZDOID>(RpcStop, OnStop);
            _registeredOn = rpc;
            _rings.Clear();
            _lastStartBySender.Clear();
            _localActive = false;
        }

        public static void Start(Player player)
        {
            EnsureRegistered();
            string style = $"{HomewardPlugin.ChannelVfxColor.Value}|{HomewardPlugin.ChannelVfxRadius.Value}";
            long target = HomewardPlugin.ChannelVfxVisibleToOthers.Value ? ZRoutedRpc.Everybody : OwnPeerId();
            _localActive = true;
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(target, RpcStart, player.GetZDOID(), player.transform.position, style);
            }
            else
            {
                OnStart(0L, player.GetZDOID(), player.transform.position, style); // single-player fallback
            }
        }

        public static void Stop(Player player)
        {
            if (!_localActive)
            {
                return;
            }
            _localActive = false;
            long target = HomewardPlugin.ChannelVfxVisibleToOthers.Value ? ZRoutedRpc.Everybody : OwnPeerId();
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(target, RpcStop, player.GetZDOID());
            }
            else
            {
                OnStop(0L, player.GetZDOID());
            }
        }

        /// <summary>Local cleanup only (logout, player gone). Nothing is broadcast.</summary>
        public static void Clear()
        {
            foreach (GameObject go in _rings.Values)
            {
                if (go != null) Object.Destroy(go);
            }
            _rings.Clear();
            _localActive = false;
        }

        private static long OwnPeerId()
        {
            // Sending to our own peer id keeps the ring local; ZRoutedRpc handles that in-process.
            return ZNet.instance != null ? ZNet.GetUID() : 0L;
        }

        // Handlers run inside the game's RPC dispatch; never let an exception escape.
        private static void OnStart(long sender, ZDOID who, Vector3 pos, string style)
        {
            try
            {
                HandleStart(sender, who, pos, style);
            }
            catch (System.Exception e)
            {
                HomewardPlugin.Log.LogWarning($"ChannelStart from {sender} failed: {e.Message}");
            }
        }

        private static void OnStop(long sender, ZDOID who)
        {
            try
            {
                if (Validate(sender, who, null, null, out _))
                {
                    Remove(who);
                }
            }
            catch (System.Exception e)
            {
                HomewardPlugin.Log.LogWarning($"ChannelStop from {sender} failed: {e.Message}");
            }
        }

        private static void HandleStart(long sender, ZDOID who, Vector3 pos, string style)
        {
            if (!Validate(sender, who, pos, style, out bool mine))
            {
                return;
            }

            // Per-sender rate limit (own rings are driven by our own code and exempt).
            if (!mine)
            {
                float now = Time.time;
                if (_lastStartBySender.TryGetValue(sender, out float last) && now - last < MinStartInterval)
                {
                    return;
                }
                _lastStartBySender[sender] = now;
            }

            Remove(who);

            if (!mine && !HomewardPlugin.ChannelVfxEnabled.Value)
            {
                return; // this client has visuals off; don't draw others' rings either
            }
            if (!mine && OtherRingCount() >= MaxOtherRings)
            {
                return;
            }

            ParseStyle(style, out Color color, out float radius);
            GameObject ring = ChannelVfx.Build(pos, color, radius,
                visuals: HomewardPlugin.ChannelVfxEnabled.Value,
                sound: mine && HomewardPlugin.ChannelSoundEnabled.Value,
                ttlSeconds: mine ? 0f : RingTtlSeconds);
            if (ring != null)
            {
                _rings[who] = ring;
            }
        }

        /// <summary>
        /// Accept a message only if the sender owns the player it claims to be, the
        /// position is sane and near that player, and the payload is small. The
        /// routed-RPC sender id is the same session id ZDO ownership uses
        /// (ZNet.cs:385), so GetOwner() == sender is a real check, not a hint.
        /// </summary>
        private static bool Validate(long sender, ZDOID who, Vector3? pos, string style, out bool mine)
        {
            Player local = Player.m_localPlayer;
            mine = local != null && who == local.GetZDOID();
            if (mine)
            {
                return true; // produced by our own code path
            }
            if (local == null || ZDOMan.instance == null)
            {
                return false;
            }
            if (style != null && style.Length > MaxStyleLength)
            {
                return false;
            }

            ZDO zdo = ZDOMan.instance.GetZDO(who);
            if (zdo == null || zdo.GetOwner() != sender)
            {
                return false; // unknown object, or the sender isn't its owner
            }

            if (pos.HasValue)
            {
                Vector3 p = pos.Value;
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)
                    || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z))
                {
                    return false;
                }
                if (Vector3.Distance(p, zdo.GetPosition()) > MaxPosDrift)
                {
                    return false; // ring must be where that player actually is
                }
                if (Vector3.Distance(p, local.transform.position) > MaxDrawDistance)
                {
                    return false; // too far to see; don't spend a particle system on it
                }
            }
            return true;
        }

        private static int OtherRingCount()
        {
            int n = 0;
            Player local = Player.m_localPlayer;
            ZDOID me = local != null ? local.GetZDOID() : ZDOID.None;
            foreach (KeyValuePair<ZDOID, GameObject> kv in _rings)
            {
                if (kv.Value != null && kv.Key != me) n++;
            }
            return n;
        }

        private static void Remove(ZDOID who)
        {
            if (_rings.TryGetValue(who, out GameObject go))
            {
                _rings.Remove(who);
                ChannelVfx.FadeOut(go);
            }
        }

        private static void ParseStyle(string style, out Color color, out float radius)
        {
            color = new Color(0.5f, 0.85f, 1f, 1f);
            radius = 1.2f;
            string[] parts = (style ?? "").Split('|');
            if (parts.Length > 0 && ColorUtility.TryParseHtmlString(parts[0], out Color c)) color = c;
            if (parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float r)) radius = Mathf.Clamp(r, 0.3f, 5f);
        }
    }
}
