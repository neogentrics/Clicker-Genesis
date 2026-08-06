using UnityEngine;

namespace ClickerGenesis.Progression
{
    /// <summary>
    /// Data-driven XP/leveling constants. Content-completion based (verse/chapter/book),
    /// not RPG/combat based. All values are placeholder defaults pending playtesting —
    /// same rule as VersePricingConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "XpConfig", menuName = "Clicker Genesis/Progression/Xp Config")]
    public class XpConfig : ScriptableObject
    {
        [SerializeField] private int xpPerVerse = 5;
        [SerializeField] private int xpPerChapterBonus = 10;
        [SerializeField] private int xpPerBookBonus = 100;
        [SerializeField] private int xpPerClickPowerUpgrade = 1;
        [Tooltip("Awarded once per bulk-buy action (5+ verses bought in one click), on top of the per-verse XP for each verse in that batch.")]
        [SerializeField] private int xpBulkBuyBonus = 20;

        [Tooltip("XP needed to go from level L to L+1 = levelXpBase * L (linear growth).")]
        [SerializeField] private int levelXpBase = 50;

        [Tooltip("Player level at which prestige becomes available.")]
        [SerializeField] private int prestigeLevelThreshold = 5;

        [Tooltip("Player level at which the (still locked) Prestige button first appears on ClickerScreen - a goal to work toward, shown well before it unlocks rather than only right at the threshold.")]
        [SerializeField] private int prestigeButtonVisibleLevel = 2;

        [Header("Reset-Prestige XP bonuses (2026-08-06)")]
        [Tooltip("Replaces xpPerVerse once the player has performed at least one Reset-Prestige.")]
        [SerializeField] private int xpPerVerseAfterReset = 10;
        [Tooltip("Replaces xpPerChapterBonus once the player has performed at least one Reset-Prestige.")]
        [SerializeField] private int xpPerChapterBonusAfterReset = 25;
        [Tooltip("Replaces xpPerBookBonus once the player has performed at least one Reset-Prestige.")]
        [SerializeField] private int xpPerBookBonusAfterReset = 150;
        [Tooltip("Tapping grants 0 XP normally - only starts granting this much per tap once the player has performed at least one Reset-Prestige.")]
        [SerializeField] private int xpPerTapAfterReset = 5;
        [Tooltip("Buying a scribe grants 0 XP normally - only starts granting this much per unit bought once the player has performed at least one Reset-Prestige.")]
        [SerializeField] private int xpPerScribePurchaseAfterReset = 1;

        public int XpPerVerse => xpPerVerse;
        public int XpPerChapterBonus => xpPerChapterBonus;
        public int XpPerBookBonus => xpPerBookBonus;
        public int XpPerClickPowerUpgrade => xpPerClickPowerUpgrade;
        public int XpBulkBuyBonus => xpBulkBuyBonus;
        public int LevelXpBase => levelXpBase;
        public int PrestigeLevelThreshold => prestigeLevelThreshold;
        public int PrestigeButtonVisibleLevel => prestigeButtonVisibleLevel;
        public int XpPerVerseAfterReset => xpPerVerseAfterReset;
        public int XpPerChapterBonusAfterReset => xpPerChapterBonusAfterReset;
        public int XpPerBookBonusAfterReset => xpPerBookBonusAfterReset;
        public int XpPerTapAfterReset => xpPerTapAfterReset;
        public int XpPerScribePurchaseAfterReset => xpPerScribePurchaseAfterReset;
    }
}
