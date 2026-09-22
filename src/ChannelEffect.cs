using UnityEngine;

namespace Homeward
{
    /// <summary>
    /// Hidden status effect that exists only to carry a borrowed vanilla VFX while
    /// the player is channeling. StatusEffect.Setup spawns m_startEffects attached
    /// to the character and Stop() destroys them, so add/remove is the whole API.
    /// </summary>
    internal class ChannelEffect : StatusEffect
    {
        internal const string EffectName = "Homeward_Channeling";

        private static ChannelEffect _template;

        internal static int Hash => Template().NameHash();

        internal static void Apply(Player player)
        {
            string source = HomewardPlugin.ChannelVfx.Value.Trim();
            if (source.Length == 0 || source.Equals("None", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EffectList effects = FindEffects(source);
            if (effects == null || !effects.HasEffects())
            {
                HomewardPlugin.Log.LogWarning($"ChannelVfx '{source}' has no start effects to borrow; channeling without VFX");
                return;
            }

            ChannelEffect template = Template();
            template.m_startEffects = effects;
            player.GetSEMan().AddStatusEffect(template);
        }

        internal static void Remove(Player player)
        {
            player.GetSEMan().RemoveStatusEffect(Hash, quiet: true);
        }

        private static ChannelEffect Template()
        {
            if (_template == null)
            {
                _template = CreateInstance<ChannelEffect>();
                _template.name = EffectName;
                _template.m_name = "Channeling";
                _template.m_hidden = true; // the action bar already shows the countdown
            }
            return _template;
        }

        /// <summary>
        /// Resolve a config name to a vanilla status effect's start effects.
        /// "Shield" is looked up by type (its asset name isn't referenced in code);
        /// everything else is an ObjectDB status effect name, e.g. Spirit, Lightning, Frost.
        /// </summary>
        private static EffectList FindEffects(string source)
        {
            if (ObjectDB.instance == null)
            {
                return null;
            }

            if (source.Equals("Shield", System.StringComparison.OrdinalIgnoreCase))
            {
                StatusEffect shield = ObjectDB.instance.m_StatusEffects.Find(se => se is SE_Shield);
                return shield != null ? shield.m_startEffects : null;
            }

            StatusEffect se2 = ObjectDB.instance.GetStatusEffect(source.GetStableHashCode());
            return se2 != null ? se2.m_startEffects : null;
        }

        // Never expire on our own; Channel removes us explicitly.
        public override bool IsDone()
        {
            return false;
        }
    }
}
