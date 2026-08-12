using System;
using System.Collections.Generic;

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

    /// <summary>One entry in a node's prerequisite list (2026-08-08 redesign) - a node id plus the
    /// rank required in it. Nodes can now require several of these simultaneously (AND), not just
    /// one, per the user's explicit ask for "some skills require 3+ other skills unlocked before
    /// they can be unlocked" - e.g. Convergence (below) requires all 8 branch capstones at once.</summary>
    [Serializable]
    public class SkillPrerequisite
    {
        public string nodeId;
        public int rankRequired;

        public SkillPrerequisite() { }
        public SkillPrerequisite(string nodeId, int rankRequired)
        {
            this.nodeId = nodeId;
            this.rankRequired = rankRequired;
        }
    }

    /// <summary>Visual shape of a node's background, distinguishing categories of skill at a glance
    /// (2026-08-08 redesign, user's explicit ask - "different shapes based off of different
    /// things"). Rendered via NodeShapeSprites (procedural, no new art assets needed).</summary>
    public enum SkillNodeShape
    {
        Circle,
        Diamond,
        Hexagon,
        Star,
        Triangle,
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

        /// <summary>Empty/null list = branch root, no prerequisite. Otherwise every entry must be
        /// satisfied (AND) before this node can be bought - single-entry for a normal chain step,
        /// multi-entry for a convergence node that needs several other skills unlocked at once
        /// (2026-08-08 redesign, replaces the old single prerequisiteId/prerequisiteRankRequired
        /// pair).</summary>
        public List<SkillPrerequisite> prerequisites = new List<SkillPrerequisite>();

        public SkillNodeShape shape = SkillNodeShape.Circle;

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

        /// <summary>When set, this node also requires the named book to be unlocked (checked via
        /// PrestigeSkillSystem/GameLoopController.Skills.IsBookUnlocked) - on top of, not instead
        /// of, its normal rank-based prerequisites (2026-08-09, "bracket" tree redesign: branches
        /// converge into a single node, then re-split into further nodes gated on which book the
        /// player has unlocked - real user request/drawing, "if they want to go further, they have
        /// to unlock a book before they can unlock the next set of skills"). Empty for every node
        /// that isn't gated this way.</summary>
        public string requiresBookResourceId;
    }
}
