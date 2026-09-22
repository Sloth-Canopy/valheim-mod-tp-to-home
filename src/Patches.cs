using HarmonyLib;
using UnityEngine;

namespace Homeward
{
    /// <summary>Taking damage cancels the channel (if CancelOnDamage).</summary>
    [HarmonyPatch(typeof(Player), "OnDamaged")]
    internal static class Player_OnDamaged_Patch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                Channel.DamageTaken = true;
            }
        }
    }

    /// <summary>
    /// Hud shows the portal swirl while this returns true. During our teleport
    /// return false so the player gets a plain fade to black instead.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ShowTeleportAnimation))]
    internal static class Player_ShowTeleportAnimation_Patch
    {
        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (!Flight.InProgress || HomewardPlugin.ShowPortalAnimation.Value || __instance != Player.m_localPlayer)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    /// <summary>
    /// Drive the game's own action bar ("Crafting..." style) while channeling.
    /// Hud hides it every frame when the player's action queue is empty, so we
    /// re-show it after the original runs.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "UpdateActionProgress")]
    internal static class Hud_UpdateActionProgress_Patch
    {
        private static void Postfix(Hud __instance, Player player)
        {
            if (!Channel.Active || player != Player.m_localPlayer)
            {
                return;
            }
            __instance.m_actionBarRoot.SetActive(true);
            __instance.m_actionProgress.SetValue(Channel.Fraction);
            __instance.m_actionName.text = $"Heading home... {Mathf.CeilToInt(Channel.Remaining)}s";
        }
    }
}
