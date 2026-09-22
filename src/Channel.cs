using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Homeward
{
    /// <summary>
    /// The "sit still for N seconds" phase. Uses the game's own sit emote so the
    /// player visibly sits, and so the game's emote-cancel-on-move logic does the
    /// movement detection for us.
    /// </summary>
    internal static class Channel
    {
        public static bool Active { get; private set; }

        // Set by the Player.OnDamaged postfix.
        internal static bool DamageTaken;

        private static float _startTime;
        private static Vector3 _startPos;

        // StartEmote writes to the ZDO; Player.UpdateEmote picks it up a frame or two
        // later and the animator then blends into the sit state. Don't demand
        // InEmote() && IsSitting() until that has had time to land.
        private const float EmoteGraceSeconds = 1.0f;
        private const float MoveTolerance = 0.5f;

        // Player.StopEmote is protected; Harmony's AccessTools gets us at it.
        private static readonly MethodInfo StopEmoteMethod = AccessTools.Method(typeof(Player), "StopEmote");

        public static float Remaining => Mathf.Max(0f, HomewardPlugin.CastSeconds.Value - (Time.time - _startTime));

        public static float Fraction
        {
            get
            {
                float cast = HomewardPlugin.CastSeconds.Value;
                return cast <= 0f ? 1f : Mathf.Clamp01(1f - Remaining / cast);
            }
        }

        public static void TryStart(Player player)
        {
            if (Active || Flight.InProgress || player.IsTeleporting())
            {
                return;
            }

            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (!profile.HaveCustomSpawnPoint())
            {
                Say(player, "You have no home.");
                return;
            }

            long cooldown = Cooldown.RemainingSeconds(player);
            if (cooldown > 0)
            {
                Say(player, $"Homeward ready in {Cooldown.Format(cooldown)}");
                return;
            }

            if (!player.IsTeleportable(HomewardPlugin.AllowWithMetal.Value))
            {
                Say(player, "Cannot travel home while carrying metal.");
                return;
            }

            if (HomewardPlugin.CastSeconds.Value <= 0f)
            {
                Depart(player);
                return;
            }

            // StartEmote will happily "succeed" while swimming or mid-air, but the
            // animator never enters the sit state there. Same rule as vanilla: you can
            // only go home from somewhere you can actually sit down.
            if (!CanSitHere(player))
            {
                Say(player, "You need solid ground to sit.");
                return;
            }

            // Refused while attacking, drawing a bow, attached to something, etc.
            if (!player.StartEmote("sit", oneshot: false))
            {
                Say(player, "Cannot travel home right now.");
                return;
            }

            Active = true;
            DamageTaken = false;
            _startTime = Time.time;
            _startPos = player.transform.position;
            HomewardPlugin.Log.LogInfo("Channel started");
        }

        public static void Update(Player player)
        {
            if (!Active)
            {
                return;
            }

            string reason = CancelReason(player);
            if (reason != null)
            {
                Cancel(player, $"Interrupted ({reason}).");
                return;
            }

            if (Remaining <= 0f)
            {
                Active = false;
                StopEmote(player);
                Depart(player);
            }
        }

        public static void Cancel(Player player, string message)
        {
            Active = false;
            StopEmote(player);
            Say(player, message);
            HomewardPlugin.Log.LogInfo($"Channel cancelled: {message}");
        }

        public static void Reset()
        {
            Active = false;
            DamageTaken = false;
        }

        private static string CancelReason(Player player)
        {
            if (player.IsDead()) return "died";
            if (DamageTaken && HomewardPlugin.CancelOnDamage.Value) return "took damage";
            if (player.InAttack() || player.IsDrawingBow() || player.IsBlocking()) return "attacked";
            if (player.InMinorAction()) return "used an item";
            if (player.InPlaceMode()) return "building";
            if (Vector3.Distance(player.transform.position, _startPos) > MoveTolerance) return "moved";
            if (!CanSitHere(player)) return "no solid ground";
            if (Time.time - _startTime > EmoteGraceSeconds)
            {
                // The game drops the emote on movement input; the animator leaves the
                // sitting state if the player is somehow not actually sitting.
                if (!player.InEmote()) return "moved";
                if (!player.IsSitting()) return "not sitting";
            }
            return null;
        }

        private static bool CanSitHere(Player player)
        {
            return player.IsOnGround()
                && !player.IsSwimming()
                && !player.IsRiding()
                && !player.IsAttached();
        }

        private static void Depart(Player player)
        {
            Vector3 target = Game.instance.GetPlayerProfile().GetCustomSpawnPoint();
            if (!player.TeleportTo(target, player.transform.rotation, distantTeleport: true))
            {
                // Not owner, already teleporting, or inside the game's 2 s teleport guard.
                HomewardPlugin.Log.LogWarning("TeleportTo refused");
                return;
            }

            Flight.Begin();
            Say(player, "Heading home...");
            HomewardPlugin.Log.LogInfo($"Heading home to {target}");
        }

        private static void StopEmote(Player player)
        {
            StopEmoteMethod?.Invoke(player, null);
        }

        private static void Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
