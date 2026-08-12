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

        /// <summary>Which book a generic "Unlock New Book" node actually unlocked, keyed by node id
        /// (2026-08-09, player-choice book unlocking redesign - see BookProgressionTreeBuilder's
        /// doc comment for why the Book Progression chain no longer earmarks a specific book per
        /// node). Only populated for BookUnlock nodes whose unlockBookResourceId is empty (the New
        /// Testament gate nodes still unlock a fixed, static group of books and never appear here).</summary>
        private readonly Dictionary<string, string> bookChoices = new Dictionary<string, string>();

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
            if (!string.IsNullOrEmpty(node.requiresBookResourceId) && !IsBookUnlocked(node.requiresBookResourceId)) return false;
            return PrerequisiteSatisfied(node);
        }

        /// <summary>Just the prerequisite-rank check, deliberately ignoring the reset gate (2026-08-06,
        /// for the Skill Tree's progressive node VISIBILITY - a reset-gated capstone should still
        /// become visible, showing "(Requires Reset)", once its prerequisite chain is maxed, rather
        /// than staying invisible until a reset happens - that would hide the very information that
        /// tells the player a reset unlocks it). IsUnlocked above is the "can this be bought right
        /// now" check (includes the reset gate); this is the separate "has the player earned the
        /// right to even SEE this node" check.</summary>
        public bool PrerequisiteSatisfied(PrestigeSkillNode node)
        {
            if (node.prerequisites == null || node.prerequisites.Count == 0) return true;
            foreach (var prereq in node.prerequisites)
                if (GetRank(prereq.nodeId) < prereq.rankRequired) return false;
            return true;
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
        /// bought - the Books tab reads this to decide what's selectable. Genesis itself is always
        /// available (the free starting-book personalization hook) and never has a tree node, so
        /// it's not covered by this check.</summary>
        public bool IsBookUnlocked(string bookResourceId)
        {
            if (config == null || string.IsNullOrEmpty(bookResourceId)) return false;

            // Player-chosen generic "Unlock New Book" nodes (2026-08-09) - checked first since
            // this is now the common case for the OT chain; NT gate nodes below stay static.
            foreach (var kvp in bookChoices)
                if (kvp.Value == bookResourceId) return true;

            foreach (var node in config.nodes)
            {
                if (node.effectType != SkillEffectType.BookUnlock || GetRank(node.id) <= 0) continue;
                if (string.IsNullOrEmpty(node.unlockBookResourceId)) continue; // generic - handled above
                // unlockBookResourceId may hold a single id or a comma-separated group (the New
                // Testament "gate" nodes unlock several books at once rather than one node per book).
                var ids = node.unlockBookResourceId.Split(',');
                foreach (var id in ids)
                    if (id.Trim() == bookResourceId)
                        return true;
            }
            return false;
        }

        /// <summary>Records which book a generic "Unlock New Book" node's rank actually unlocked
        /// (2026-08-09, player-choice redesign). Must only be called once the node itself has been
        /// bought (i.e. paired with Buy(node) in the same purchase action) - a bought-but-unchosen
        /// node grants access to nothing until this is called, which is why the confirm/purchase UI
        /// picks the book BEFORE spending Grace rather than after.</summary>
        public void ChooseBook(string nodeId, string bookResourceId)
        {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(bookResourceId)) return;
            bookChoices[nodeId] = bookResourceId;
        }

        /// <summary>The book a generic node's rank was spent on, or null if not yet chosen (should
        /// only be transiently true mid-purchase, never after - see ChooseBook's doc comment) or if
        /// this isn't a generic book-unlock node at all. Feeds the tree tooltip's "Unlocked: X" line
        /// once a slot has been spent.</summary>
        public string GetChosenBook(string nodeId) =>
            bookChoices.TryGetValue(nodeId, out var id) ? id : null;

        /// <summary>Every node id -> chosen book resource id pair (2026-08-09, save system).</summary>
        public IEnumerable<KeyValuePair<string, string>> ExportBookChoices() => bookChoices;

        /// <summary>Restores book-choice state from a save file (2026-08-09) - counterpart to
        /// LoadState's rank restoration below, called alongside it.</summary>
        public void LoadBookChoices(IEnumerable<KeyValuePair<string, string>> savedChoices)
        {
            bookChoices.Clear();
            if (savedChoices == null) return;
            foreach (var kvp in savedChoices)
                if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                    bookChoices[kvp.Key] = kvp.Value;
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

        /// <summary>How many distinct nodes have at least one rank bought - the Stats screen's
        /// "skills bought" figure (2026-08-06). Counts nodes, not total ranks, so a 5-rank node
        /// bought to rank 3 still counts once, not three times.</summary>
        public int PurchasedNodeCount()
        {
            if (config == null) return 0;
            int count = 0;
            foreach (var node in config.nodes)
                if (GetRank(node.id) > 0) count++;
            return count;
        }

        /// <summary>Every node with at least one rank bought, in config order - feeds the
        /// permanent-upgrades panel on ClickerScreen (2026-08-06), which lists what a player has
        /// actually unlocked instead of cramming that text into the Scribes/Managers rows.</summary>
        public List<(PrestigeSkillNode node, int rank)> GetAllPurchased()
        {
            var result = new List<(PrestigeSkillNode, int)>();
            if (config == null) return result;
            foreach (var node in config.nodes)
            {
                int rank = GetRank(node.id);
                if (rank > 0) result.Add((node, rank));
            }
            return result;
        }

        /// <summary>Every purchased node id + its rank, regardless of whether it still resolves
        /// against the current config (2026-08-08, save system) - unlike GetAllPurchased above,
        /// this doesn't filter through config.nodes, so a save written against an older tree
        /// shape still round-trips its raw rank data even if a node was since renamed/removed
        /// (SaveMigrator's job to reconcile that, not this export).</summary>
        public IEnumerable<KeyValuePair<string, int>> ExportRanks() => ranks;

        /// <summary>Restores rank state from a save file (2026-08-08). Silently ignores node ids
        /// that no longer exist in the current config - a real day-one bit of forward compat, not
        /// an edge case, since this tree is explicitly expected to keep growing.</summary>
        public void LoadState(IEnumerable<KeyValuePair<string, int>> savedRanks)
        {
            ranks.Clear();
            if (savedRanks == null) return;
            foreach (var kvp in savedRanks)
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value > 0)
                    ranks[kvp.Key] = kvp.Value;
        }
    }
}
