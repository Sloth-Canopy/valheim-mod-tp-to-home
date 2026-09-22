using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Homeward
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class HomewardPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "canpoy.homeward";
        public const string PluginName = "Homeward";
        public const string PluginVersion = "0.1.0";

        // Key in Player.m_customData. Value is unix seconds (UTC) of the last completed homeward.
        internal const string LastUsedKey = "homeward.lastUsed";

        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyboardShortcut> Hotkey;
        internal static ConfigEntry<int> CooldownMinutes;
        internal static ConfigEntry<bool> AllowWithMetal;

        private Harmony _harmony;

        // Set when we've kicked off a teleport and are waiting for the player to arrive.
        // The cooldown is only stamped on arrival (decisions.md #7).
        private bool _awaitingArrival;

        private void Awake()
        {
            Log = Logger;

            Hotkey = Config.Bind("General", "Hotkey", new KeyboardShortcut(KeyCode.H),
                "Key that starts the journey home.");
            CooldownMinutes = Config.Bind("General", "CooldownMinutes", 60,
                new ConfigDescription("Real-time minutes between uses. Survives logout.",
                    new AcceptableValueRange<int>(0, 24 * 60)));
            AllowWithMetal = Config.Bind("General", "AllowWithMetal", false,
                "Ignore the portal metal restriction. Normal portal rules apply when false.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                _awaitingArrival = false;
                return;
            }

            if (_awaitingArrival && !player.IsTeleporting())
            {
                _awaitingArrival = false;
                StampCooldown(player);
                Log.LogInfo("Arrived home, cooldown started");
            }

            if (IsTyping() || !Hotkey.Value.IsDown())
            {
                return;
            }

            TryHomeward(player);
        }

        private static bool IsTyping()
        {
            return Menu.IsVisible()
                || Console.IsVisible()
                || TextInput.IsVisible()
                || (Chat.instance != null && Chat.instance.HasFocus());
        }

        private void TryHomeward(Player player)
        {
            if (player.IsTeleporting())
            {
                return;
            }

            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (!profile.HaveCustomSpawnPoint())
            {
                player.Message(MessageHud.MessageType.Center, "You have no home.");
                return;
            }

            long remaining = CooldownRemainingSeconds(player);
            if (remaining > 0)
            {
                player.Message(MessageHud.MessageType.Center, $"Homeward ready in {FormatDuration(remaining)}");
                return;
            }

            if (!player.IsTeleportable(AllowWithMetal.Value))
            {
                player.Message(MessageHud.MessageType.Center, "Cannot travel home while carrying metal.");
                return;
            }

            Vector3 target = profile.GetCustomSpawnPoint();
            if (!player.TeleportTo(target, player.transform.rotation, distantTeleport: true))
            {
                // Not owner, already teleporting, or inside the game's 2 s teleport guard.
                Log.LogWarning("TeleportTo refused");
                return;
            }

            _awaitingArrival = true;
            player.Message(MessageHud.MessageType.Center, "Heading home...");
            Log.LogInfo($"Heading home to {target}");
        }

        private static long NowSeconds()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private static void StampCooldown(Player player)
        {
            player.m_customData[LastUsedKey] = NowSeconds().ToString();
        }

        private static long CooldownRemainingSeconds(Player player)
        {
            if (!player.m_customData.TryGetValue(LastUsedKey, out string raw) || !long.TryParse(raw, out long lastUsed))
            {
                return 0;
            }
            long readyAt = lastUsed + CooldownMinutes.Value * 60L;
            return System.Math.Max(0, readyAt - NowSeconds());
        }

        private static string FormatDuration(long seconds)
        {
            long m = seconds / 60;
            long s = seconds % 60;
            return m > 0 ? $"{m}m {s:00}s" : $"{s}s";
        }
    }
}
