using System.Collections.Generic;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// Runtime state for the redesigned tree - owned ranks per node, unlocked books, and effect
    /// aggregation. Phase 3 (2026-08-09, real-economy integration) rewired this off its original
    /// sandbox-only local Grace double onto the REAL PrestigeSystem instance GameLoopController
    /// already owns - Grace purchased here is the player's actual saved Grace balance, and
    /// GetTotalEffect below is what GameLoopController's Effective* formulas now read from
    /// alongside the old (still-live) PrestigeSkillSystem, so V2 tree purchases actually move the
    /// needle on real Ink/sec, tap value, pricing, etc.
    ///
    /// Mirrors the split PrestigeSkillTreeConfig/PrestigeSkillSystem already keeps in the shipped
    /// tree: SkillNodeData/SkillTreeDatabase describe the graph, this tracks what a specific player
    /// has actually bought.
    /// </summary>
    public class SkillTreeRuntimeState
    {
        private readonly Dictionary<SkillNodeData, int> ranks = new Dictionary<SkillNodeData, int>();
        private readonly HashSet<string> unlockedBookIds = new HashSet<string>();
        private readonly PrestigeSystem prestige;
        private readonly SkillTreeDatabase database;

        public double Grace => prestige.Grace;

        /// <summary>Exposed (2026-08-09) so SkillTreeUIManager can read its database FROM this
        /// runtime state instead of holding a second, independent [SerializeField] reference of
        /// its own - two separately-wired references to "the database" were a real footgun (one
        /// authoring pass updated GameLoopController's copy but not the scene's UI manager copy,
        /// and the tree silently kept rendering stale content). Single source of truth now.</summary>
        public SkillTreeDatabase Database => database;

        public SkillTreeRuntimeState(PrestigeSystem prestige, SkillTreeDatabase database)
        {
            this.prestige = prestige;
            this.database = database;
        }

        public int GetRank(SkillNodeData node) => node != null && ranks.TryGetValue(node, out var r) ? r : 0;

        public bool IsMaxed(SkillNodeData node) => node != null && GetRank(node) >= node.maxRank;

        public bool IsBookUnlocked(string bookResourceId) =>
            string.IsNullOrEmpty(bookResourceId) || unlockedBookIds.Contains(bookResourceId);

        /// <summary>The gates a node carries beyond its rank-prerequisite graph - reset-prestige
        /// gating (now read from the real PrestigeSystem.ResetPrestigeCount, not a settable stub)
        /// and book-active gating.</summary>
        public bool ExtraGatesSatisfied(SkillNodeData node)
        {
            if (node == null) return false;
            if (node.requiresResetPrestige && prestige.ResetPrestigeCount <= 0) return false;
            if (!string.IsNullOrEmpty(node.requiresBookResourceId) && !IsBookUnlocked(node.requiresBookResourceId)) return false;
            return true;
        }

        public bool CanAfford(SkillNodeData node) => node != null && Grace >= node.GetNextCost(GetRank(node));

        public bool CanBuy(SkillNodeData node)
        {
            if (node == null || IsMaxed(node)) return false;
            if (!node.PrerequisitesSatisfied(GetRank)) return false;
            if (!ExtraGatesSatisfied(node)) return false;
            return CanAfford(node);
        }

        public bool TryBuy(SkillNodeData node)
        {
            if (!CanBuy(node)) return false;
            if (!prestige.TrySpendGrace(node.GetNextCost(GetRank(node)))) return false;
            ranks[node] = GetRank(node) + 1;
            return true;
        }

        /// <summary>The real Book Progression formula - round(30 x 1.22^n), n = how many books
        /// have already been unlocked this save. Deliberately NOT read from the book's own
        /// authored BookMasteryData.slotIndex/GetUnlockCost() (2026-08-09 fix) - the real design
        /// (Grace-Skill-Tree.html) is 38 generic, interchangeable slots where cost climbs with
        /// PURCHASE ORDER, not with which book you pick each time; a per-book fixed cost would let
        /// a player unlock cheap-slot books out of order for less Grace than intended.</summary>
        public double NextBookUnlockCost =>
            System.Math.Round(BookMasteryData.SlotCostBase * System.Math.Pow(BookMasteryData.SlotCostGrowth, unlockedBookIds.Count));

        public bool TryUnlockBook(BookMasteryData book)
        {
            if (book == null || unlockedBookIds.Contains(book.bookResourceId)) return false;
            if (!prestige.TrySpendGrace(NextBookUnlockCost)) return false;
            unlockedBookIds.Add(book.bookResourceId);
            return true;
        }

        /// <summary>Sum of (rank * effectPerRank) across every owned node of the given effect type -
        /// GameLoopController adds this into the same Effective* formulas that already read the old
        /// tree's PrestigeSkillSystem.GetTotalEffect, so both trees' bonuses stack additively.</summary>
        public double GetTotalEffect(SkillEffectType type)
        {
            double total = 0;
            foreach (var kvp in ranks)
                if (kvp.Key != null && kvp.Key.effectType == type)
                    total += kvp.Value * kvp.Key.effectPerRank;
            return total;
        }

        /// <summary>Every purchased node's stable Id + its rank, for CaptureSaveData.</summary>
        public IEnumerable<KeyValuePair<string, int>> ExportRanks()
        {
            foreach (var kvp in ranks)
                if (kvp.Key != null) yield return new KeyValuePair<string, int>(kvp.Key.Id, kvp.Value);
        }

        public IEnumerable<string> ExportUnlockedBooks() => unlockedBookIds;

        /// <summary>Restores rank state from a save file, resolving each saved Id back to a real
        /// SkillNodeData via the database. Silently skips ids that no longer resolve (a node
        /// renamed/removed since the save was written) - same forward-compat rule the old tree's
        /// PrestigeSkillSystem.LoadState already follows.</summary>
        public void LoadState(IEnumerable<KeyValuePair<string, int>> savedRanks)
        {
            ranks.Clear();
            if (savedRanks == null || database == null) return;
            var byId = new Dictionary<string, SkillNodeData>();
            foreach (var node in database.GetAllNodes())
                if (node != null && !byId.ContainsKey(node.Id)) byId[node.Id] = node;

            foreach (var kvp in savedRanks)
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value > 0 && byId.TryGetValue(kvp.Key, out var node))
                    ranks[node] = kvp.Value;
        }

        /// <summary>Adds to the unlocked-book set rather than replacing it (2026-08-09) - the
        /// player's free starting book is seeded directly by GameLoopController.BuildFreshState()
        /// before a save (if any) is applied, and a save's own list should extend that seed, not
        /// wipe it. A fresh save's empty list is a safe no-op either way.</summary>
        public void LoadUnlockedBooks(IEnumerable<string> savedBookIds)
        {
            if (savedBookIds == null) return;
            foreach (var id in savedBookIds)
                if (!string.IsNullOrEmpty(id)) unlockedBookIds.Add(id);
        }
    }
}
