using UnityEngine;

namespace Homeward
{
    /// <summary>
    /// Buff-bar icon for the cooldown. It owns no state of its own: expiry and the
    /// countdown text both read Cooldown.RemainingSeconds (Player.m_customData), so
    /// it stays correct across logout/login and is simply re-added when missing.
    /// </summary>
    internal class CooldownEffect : StatusEffect
    {
        internal const string EffectName = "Homeward_Cooldown";

        private static CooldownEffect _template;

        internal static int Hash => Template().NameHash();

        /// <summary>Adds the icon to the player if they are on cooldown and don't have it yet.</summary>
        internal static void Sync(Player player)
        {
            if (Cooldown.RemainingSeconds(player) <= 0)
            {
                return; // SEMan removes it itself once IsDone() reports true
            }
            SEMan seman = player.GetSEMan();
            if (seman.HaveStatusEffect(Hash))
            {
                return;
            }
            CooldownEffect template = Template();
            template.m_icon = BedIcon(); // fetched per add: prefabs aren't around before a world loads
            seman.AddStatusEffect(template);
        }

        private static CooldownEffect Template()
        {
            if (_template == null)
            {
                _template = CreateInstance<CooldownEffect>();
                _template.name = EffectName; // NameHash() derives from this
                _template.m_name = "Homeward";
                _template.m_tooltip = "You recently travelled home. Wait for the cooldown to use Homeward again.";
                _template.m_cooldownIcon = true; // Hud draws its grey cooldown overlay
            }
            return _template;
        }

        private static Sprite BedIcon()
        {
            GameObject bed = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("bed") : null;
            Piece piece = bed != null ? bed.GetComponent<Piece>() : null;
            return piece != null ? piece.m_icon : null;
        }

        private Player Owner => m_character as Player;

        public override bool IsDone()
        {
            return Owner == null || Cooldown.RemainingSeconds(Owner) <= 0;
        }

        public override string GetIconText()
        {
            return Owner == null ? "" : GetTimeString(Cooldown.RemainingSeconds(Owner));
        }

        public override string GetTooltipString()
        {
            return m_tooltip;
        }
    }
}
