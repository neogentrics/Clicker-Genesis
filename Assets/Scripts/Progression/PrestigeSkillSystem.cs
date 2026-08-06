using System;
using System.Collections.Generic;

namespace ClickerGenesis.Progression
{
    /// <summary>
    /// Tracks purchased ranks per Grace skill node and aggregates their effects. Never resets on
    /// prestige (free or opt-in reset path) - skills are permanent per the user's explicit spec
    /// (2026-08-04): "any skill they unlock is like a permanent upgrade." Lives inside
    /// GameLoopController's persistent singleton alongside PrestigeSystem, but is its own plain
    /// class (same testable-without-Unity pattern as InkWallet/LevelSystem/ScribeSystem).
    ///
    /// This supersedes PrestigeSystem's earlier lean-v1 "every Grace ever spent grants +1% Ink"
    /// auto-bonus - that placeholder was explicitly flagged in the original spec as a stand-in
    /// "until a real Grace Shop is proven out." This *is* that real shop, so the auto-bonus is
    /// removed rather than stacked alongside it (double-dipping on the same Grace-spent number
    /// would be confusing, not additive value).
    /// </summary>
    public class PrestigeSkillSystem
    {
        private readonly PrestigeSkillTreeConfig config;
        private readonly Dictionary<string, int> ranks = new Dictionary<string, int>();

        public PrestigeSkillSystem(PrestigeSkillTreeConfig config)
        {
            this.config = config;
        }

        public int GetRank(string nodeId) => ranks.TryGetValue(nodeId, out var r) ? r : 0;

        public bool IsMaxed(PrestigeSkillNode node) => GetRank(node.id) >= node.maxRank;

        /// <summary>A node is reachable once its prerequisite (if any) has enough ranks - the
        /// "requires 3-4 upgrades on the previous skill before the next one unlocks" behavior -
        /// AND, for reset-gated nodes, only once the player has performed at least one opt-in
        /// Reset-Prestige (2026-08-06), regardless of prerequisite state.</summary>
        public bool IsUnlocked(PrestigeSkillNode node, bool hasResetPrestiged)
        {
            if (node.requiresResetPrestige && !hasResetPrestiged) return false;
            if (string.IsNullOrEmpty(node.prerequisiteId)) return true;
            return GetRank(node.prerequisiteId) >= node.prerequisiteRankRequired;
        }

        public double GetNextCost(PrestigeSkillNode node)
        {
            int rank = GetRank(node.id);
            return node.baseCost * Math.Pow(node.costGrowthPerRank, rank);
        }

        public bool CanBuy(PrestigeSkillNode node, double graceAvailable, bool hasResetPrestiged)
        {
            if (IsMaxed(node) || !IsUnlocked(node, hasResetPrestiged)) return false;
            return graceAvailable >= GetNextCost(node);
        }

        /// <summary>Returns the Grace cost that should be deducted by the caller (GameLoopController,
        /// via PrestigeSystem.TrySpendGrace) - this class only tracks ranks, it never touches the
        /// Grace balance itself.</summary>
        public double Buy(PrestigeSkillNode node)
        {
            double cost = GetNextCost(node);
            ranks[node.id] = GetRank(node.id) + 1;
            return cost;
        }

        /// <summary>True once the skill node granting access to this book resource has been
        /// bought - the Books tab (not yet built) reads this to decide what's selectable. Genesis
        /// itself is always available (the free starting-book personalization hook) and never has
        /// a tree node, so it's not covered by this check.</summary>
        public bool IsBookUnlocked(string bookResourceId)
        {
            if (config == null || string.IsNullOrEmpty(bookResourceId)) return false;
            foreach (var node in config.nodes)
            {
                if (node.effectType != SkillEffectType.BookUnlock || GetRank(node.id) <= 0) continue;
                // unlockBookResourceId may hold a single id or a comma-separated group (the New
                // Testament "gate" nodes unlock several books at once rather than one node per book).
                var ids = node.unlockBookResourceId.Split(',');
                foreach (var id in ids)
                    if (id.Trim() == bookResourceId)
                        return true;
            }
            return false;
        }

        /// <summary>Sum of (rank * effectPerRank) across every node of the given effect type -
        /// GameLoopController folds this into the relevant formula (income, tap value, etc.).</summary>
        public double GetTotalEffect(SkillEffectType type)
        {
            if (config == null) return 0;
            double total = 0;
            foreach (var node in config.nodes)
                if (node.effectType == type)
                    total += GetRank(node.id) * node.effectPerRank;
            return total;
        }
    }
}
