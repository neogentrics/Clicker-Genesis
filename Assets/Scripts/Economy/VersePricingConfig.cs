using UnityEngine;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// Data-driven pricing parameters for verse_cost(n) = baseCost * growthRate^n.
    /// One config can be shared across books, or each book can carry its own for
    /// per-book difficulty tuning after playtesting.
    /// </summary>
    [CreateAssetMenu(fileName = "VersePricingConfig", menuName = "Clicker Genesis/Economy/Verse Pricing Config")]
    public class VersePricingConfig : ScriptableObject
    {
        [SerializeField] private double baseCost = 10;
        [SerializeField] private double growthRate = 1.12;
        [SerializeField, Range(0f, 1f)] private float chapterBulkDiscount = 0.25f;

        public double BaseCost => baseCost;
        public double GrowthRate => growthRate;

        /// <summary>Fraction knocked off a chapter bulk-buy, e.g. 0.25 = 25% discount.</summary>
        public float ChapterBulkDiscount => chapterBulkDiscount;
    }
}
