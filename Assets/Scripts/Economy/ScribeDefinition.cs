using UnityEngine;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// One scribe tier within a book's roster. Data-only — actual owned-count/cost-curve state
    /// lives in ScribeSystem, matching the InkWallet/VersePricingConfig split.
    /// </summary>
    [System.Serializable]
    public class ScribeDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string flavorText;

        public double baseCost;
        public double costGrowthRate;
        public double baseInkPerSecond;

        [Tooltip("Verse index (within the book) the player must have reached to unlock this tier. 0 = unlocked from the start.")]
        public int unlockAtVerseIndex;

        [Header("Manager (optional — leave managerName empty for tiers with no manager)")]
        public string managerName;
        [TextArea] public string managerFlavorText;

        [Tooltip("Player level required for this tier's manager to activate. 0 = no manager on this tier.")]
        public int managerUnlockLevel;

        [Tooltip("Output multiplier bonus while the manager is active, e.g. 0.25 = +25%.")]
        public float managerBonusMultiplier = 0.25f;

        [Tooltip("Ink cost to unlock this manager once the level threshold is reached (REVISED 2026-08-04 - managers no longer auto-unlock by level alone). 0 = free (Adam, the first manager, per the personalization-hook pattern used for the free starting book).")]
        public double managerUnlockCost;

        public bool HasManager => managerUnlockLevel > 0 && !string.IsNullOrEmpty(managerName);
    }
}
