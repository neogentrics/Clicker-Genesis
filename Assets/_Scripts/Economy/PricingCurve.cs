using System;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// verse_cost(n) = baseCost * growthRate^n, where n is the verse's index within
    /// the player's currently active book (resets to 0 for each newly unlocked book).
    /// </summary>
    public static class PricingCurve
    {
        public static double VerseCost(VersePricingConfig config, int verseIndexInBook)
        {
            return config.BaseCost * Math.Pow(config.GrowthRate, verseIndexInBook);
        }

        /// <summary>
        /// Sum of remaining verse costs in a chapter, discounted by config.ChapterBulkDiscount.
        /// </summary>
        public static double ChapterBulkCost(VersePricingConfig config, int firstVerseIndexInBook, int remainingVerseCount)
        {
            double sum = 0;
            for (int i = 0; i < remainingVerseCount; i++)
            {
                sum += VerseCost(config, firstVerseIndexInBook + i);
            }
            return sum * (1.0 - config.ChapterBulkDiscount);
        }
    }
}
