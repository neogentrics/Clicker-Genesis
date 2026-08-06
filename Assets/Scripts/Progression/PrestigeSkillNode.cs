using System;

namespace ClickerGenesis.Progression
{
    /// <summary>Which part of the economy a skill's rank bonus applies to. Every type maps to a
    /// real, already-existing formula in GameLoopController/ScribeSystem/PrestigeSystem - no
    /// invented subsystem is required to make a skill do something.</summary>
    public enum SkillEffectType
    {
        IncomeMultiplier,
        ClickPowerMultiplier,
        ManagerBonusBoost,
        ProgressMultiplierBoost,
        ScribeMilestoneBoost,
        GraceGainBonus,
        PricingDiscount,
        ManagerUnlockLevelDiscount,

        /// <summary>Grants access to a book rather than a stat bonus - effectPerRank is unused,
        /// unlockBookResourceId names the book instead (see PrestigeSkillNode.unlockBookResourceId).</summary>
        BookUnlock,
    }

    /// <summary>
    /// One node in the Grace skill tree - a permanent, multi-rank upgrade bought with Grace.
    /// Never resets on prestige (free or reset path) - "any skill they unlock is like a permanent
    /// upgrade" (2026-08-04, user's explicit spec for this system). Plain serializable data, held
    /// in a list inside PrestigeSkillTreeConfig rather than one ScriptableObject per node - with
    /// ~100 nodes, one asset per node would be unmanageable.
    /// </summary>
    [Serializable]
    public class PrestigeSkillNode
    {
        public string id;
        public string displayName;
        public string description;
        public string branch;

        /// <summary>Empty = branch root, no prerequisite. Otherwise the id of another node in the
        /// same or a different branch.</summary>
        public string prerequisiteId;

        /// <summary>Ranks required in the prerequisite before this node can be bought at all.</summary>
        public int prerequisiteRankRequired;

        public int maxRank;
        public double baseCost;
        public double costGrowthPerRank;

        public SkillEffectType effectType;

        /// <summary>Bonus granted per rank, in the units GameLoopController expects for that
        /// effect type (e.g. 0.02 = +2% for a multiplier-style effect).</summary>
        public double effectPerRank;

        /// <summary>Only set when effectType == BookUnlock - the book's Resources/Verses/{id}.json
        /// resource name (without extension), e.g. "exodus_2". Empty for every other effect type.</summary>
        public string unlockBookResourceId;

        /// <summary>Only set when effectType == BookUnlock - the display name for the Books tab /
        /// tree tooltip, e.g. "Exodus". Empty for every other effect type.</summary>
        public string unlockBookDisplayName;

        /// <summary>Capstones sit at the end of a branch's chain (prerequisite = that branch's
        /// last rank-chain node, maxed) and grant a single larger rank-1 bonus rather than a long
        /// incremental chain.</summary>
        public bool isCapstone;

        /// <summary>When true, this node can never be bought until the player has performed at
        /// least one opt-in Reset-Prestige (PrestigeSystem.ResetPrestigeCount &gt; 0) - even once
        /// its normal prerequisite chain is otherwise satisfied. Reserved for the biggest payoffs
        /// (2026-08-06, user's explicit ask: "some skills could be locked behind doing a complete
        /// reset instead of a free reset"), so the deepest tree investment requires having reset
        /// at least once, not just accumulated enough Grace from Free prestiges.</summary>
        public bool requiresResetPrestige;
    }
}
