namespace Homeward
{
    /// <summary>
    /// Real-time cooldown persisted in Player.m_customData as unix seconds (UTC).
    /// </summary>
    internal static class Cooldown
    {
        internal const string LastUsedKey = "homeward.lastUsed";

        public static void Stamp(Player player)
        {
            player.m_customData[LastUsedKey] = NowSeconds().ToString();
        }

        public static long RemainingSeconds(Player player)
        {
            if (!player.m_customData.TryGetValue(LastUsedKey, out string raw) || !long.TryParse(raw, out long lastUsed))
            {
                return 0;
            }
            long readyAt = lastUsed + HomewardPlugin.CooldownSeconds.Value;
            return System.Math.Max(0, readyAt - NowSeconds());
        }

        public static string Format(long seconds)
        {
            long m = seconds / 60;
            long s = seconds % 60;
            return m > 0 ? $"{m}m {s:00}s" : $"{s}s";
        }

        private static long NowSeconds()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
