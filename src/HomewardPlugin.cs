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
        public const string PluginVersion = "0.5.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyboardShortcut> Hotkey;
        internal static ConfigEntry<int> CooldownSeconds;
        internal static ConfigEntry<float> CastSeconds;
        internal static ConfigEntry<bool> AllowWithMetal;
        internal static ConfigEntry<bool> CancelOnDamage;
        internal static ConfigEntry<bool> ShowPortalAnimation;
        internal static ConfigEntry<bool> ChannelVfxEnabled;
        internal static ConfigEntry<string> ChannelVfxColor;
        internal static ConfigEntry<float> ChannelVfxRadius;
        internal static ConfigEntry<bool> ChannelSoundEnabled;
        internal static ConfigEntry<float> ChannelSoundVolume;

        private Harmony _harmony;
        private bool _wasOnCooldown;

        private void Awake()
        {
            Log = Logger;

            Hotkey = Config.Bind("General", "Hotkey", new KeyboardShortcut(KeyCode.H),
                "Key that starts the journey home. Press again while channeling to cancel.");
            CooldownSeconds = Config.Bind("General", "CooldownSeconds", 3600,
                new ConfigDescription("Real-time seconds between uses (3600 = 1 hour). Survives logout.",
                    new AcceptableValueRange<int>(0, 7 * 24 * 3600)));
            CastSeconds = Config.Bind("General", "CastSeconds", 8f,
                new ConfigDescription("Seconds you must sit still before the teleport fires. 0 = instant.",
                    new AcceptableValueRange<float>(0f, 60f)));
            AllowWithMetal = Config.Bind("General", "AllowWithMetal", false,
                "Ignore the portal metal restriction. Normal portal rules apply when false.");
            CancelOnDamage = Config.Bind("General", "CancelOnDamage", true,
                "Taking damage while channeling cancels the teleport.");
            ShowPortalAnimation = Config.Bind("General", "ShowPortalAnimation", false,
                "Show the vanilla portal swirl during the teleport. When false you get a plain fade to black.");

            ChannelVfxEnabled = Config.Bind("Visuals", "ChannelVfxEnabled", true,
                "Show the rune ring and motes under you while channeling.");
            ChannelVfxColor = Config.Bind("Visuals", "ChannelVfxColor", "#7FD7FF",
                "Tint of the ring and motes, as an HTML color (#RRGGBB or #RRGGBBAA).");
            ChannelVfxRadius = Config.Bind("Visuals", "ChannelVfxRadius", 1.2f,
                new ConfigDescription("Radius of the ring in meters.", new AcceptableValueRange<float>(0.3f, 5f)));
            ChannelSoundEnabled = Config.Bind("Visuals", "ChannelSoundEnabled", true,
                "Play a gentle drone-and-chimes loop while channeling.");
            ChannelSoundVolume = Config.Bind("Visuals", "ChannelSoundVolume", 0.5f,
                new ConfigDescription("Volume of the channeling sound (also scaled by the game's SFX slider).", new AcceptableValueRange<float>(0f, 1f)));

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
                Channel.Reset();
                Flight.Reset();
                return;
            }

            Flight.Update(player);
            Channel.Update(player);
            UpdateCooldownFeedback(player);

            if (IsTyping() || !Hotkey.Value.IsDown())
            {
                return;
            }

            if (Channel.Active)
            {
                Channel.Cancel(player, "Cancelled.");
            }
            else
            {
                Channel.TryStart(player);
            }
        }

        private void UpdateCooldownFeedback(Player player)
        {
            bool onCooldown = Cooldown.RemainingSeconds(player) > 0;
            CooldownEffect.Sync(player);
            if (_wasOnCooldown && !onCooldown && !player.IsDead())
            {
                player.Message(MessageHud.MessageType.TopLeft, "Homeward is ready.");
            }
            _wasOnCooldown = onCooldown;
        }

        private static bool IsTyping()
        {
            return Menu.IsVisible()
                || Console.IsVisible()
                || TextInput.IsVisible()
                || (Chat.instance != null && Chat.instance.HasFocus());
        }
    }
}
