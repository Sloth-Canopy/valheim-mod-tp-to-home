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

        private static ZRoutedRpc _registeredOn;
        private static readonly Dictionary<ZDOID, GameObject> _rings = new Dictionary<ZDOID, GameObject>();
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

        private static void OnStart(long sender, ZDOID who, Vector3 pos, string style)
        {
            Remove(who);
            bool mine = Player.m_localPlayer != null && who == Player.m_localPlayer.GetZDOID();
            if (!mine && !HomewardPlugin.ChannelVfxEnabled.Value)
            {
                return; // this client has visuals off; don't draw others' rings either
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

        private static void OnStop(long sender, ZDOID who)
        {
            Remove(who);
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
