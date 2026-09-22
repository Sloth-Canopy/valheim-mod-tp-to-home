namespace Homeward
{
    /// <summary>
    /// Tracks the teleport itself, from TeleportTo() until the player has arrived.
    /// The cooldown is stamped on arrival, never on departure (decisions.md #7).
    /// </summary>
    internal static class Flight
    {
        public static bool InProgress { get; private set; }

        public static void Begin()
        {
            InProgress = true;
        }

        public static void Update(Player player)
        {
            if (!InProgress || player.IsTeleporting())
            {
                return;
            }

            InProgress = false;
            Channel.EndPose(player); // stand up while the screen is still black
            Cooldown.Stamp(player);
            HomewardPlugin.Log.LogInfo("Arrived home, cooldown started");
        }

        public static void Reset()
        {
            InProgress = false;
        }
    }
}
