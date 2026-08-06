using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// Minor Bible character attached to a manager's tier (2026-08-06, built per explicit user
    /// authorization from Assets/Resources/Bible/CharacterIndex/Genesis.json's submanager data).
    /// Grants either a scribe-cost reduction ("cost-cutter") or a boost to the parent manager's
    /// own output-bonus multiplier ("loyalty-boost") once hired. Gated the same way a tier's own
    /// manager is - reaching the character's real first-mention verse, then an Ink cost.
    /// </summary>
    [System.Serializable]
    public class SubmanagerDefinition
    {
        public string id;
        public string displayName;

        [Tooltip("\"cost-cutter\" (reduces this tier's next scribe cost by perkAmount) or \"loyalty-boost\" (adds perkAmount to the parent manager's own output-bonus multiplier).")]
        public string perkFlavor;

        [Tooltip("Percentage effect, e.g. 0.05 = 5%. Placeholder pending playtesting, same rule as every other numeric constant in this project.")]
        public float perkAmount = 0.05f;

        [Tooltip("Verse index (within Genesis) this character's own first scriptural mention falls at - computed from CharacterIndex/Genesis.json, not invented.")]
        public int unlockAtVerseIndex;

        [Tooltip("Ink cost to hire this submanager once their verse is reached.")]
        public double unlockCost;
    }

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

        [Header("Submanagers (optional, 2026-08-06 - see SubmanagerDefinition)")]
        public List<SubmanagerDefinition> submanagers = new List<SubmanagerDefinition>();

        public bool HasManager => managerUnlockLevel > 0 && !string.IsNullOrEmpty(managerName);
    }
}
