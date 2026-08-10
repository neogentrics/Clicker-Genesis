namespace ClickerGenesis.Economy
{
    /// <summary>
    /// Shared "buy enough of this and it gets permanently stronger" curve — the
    /// 10/25/50/100-owned breakpoint pattern from most idle games (AdVenture Capitalist, etc),
    /// used by both scribe production and Click Power. Placeholder thresholds/multipliers,
    /// same rule as every other numeric constant in this project — pending playtesting.
    /// </summary>
    public static class MilestoneCurve
    {
        /// <summary>Hard cap on how many of a single scribe tier a player can ever own (2026-08-09,
        /// user's explicit design call - scribes previously had no max at all). Breakpoints below
        /// are scaled to land exactly on this cap at the max multiplier.</summary>
        public const int MaxOwned = 300;

        private static readonly (int threshold, float multiplier)[] Breakpoints =
        {
            (10, 1.2f),
            (25, 2f),
            (50, 3f),
            (100, 4f),
            (200, 4.5f),
            (300, 5f),
        };

        public static float GetMultiplier(int count)
        {
            float multiplier = 1f;
            foreach (var (threshold, mult) in Breakpoints)
                if (count >= threshold) multiplier = mult;
            return multiplier;
        }
    }
}
