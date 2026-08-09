using System.Collections.Generic;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// Plain runtime state for the redesigned tree - owned ranks per node, unlocked books, and a
    /// local Grace balance. Mirrors the split the shipped tree already keeps between data
    /// (PrestigeSkillTreeConfig) and state (PrestigeSkillSystem): SkillNodeData/BookMasteryData
    /// describe the graph, this tracks what a specific player has actually bought.
    ///
    /// Phase 2 scope note: Grace here is a local double this class owns directly, NOT yet wired to
    /// the real PrestigeSystem/GameLoopController economy - that integration (real Grace balance,
    /// real save/load, ResetPrestigeCount) is deliberately deferred to whichever phase actually
    /// plugs this tree into the live game, so Phase 2's UI can be built and demoed against a
    /// self-contained stand-in without touching production save data.
    /// </summary>
    public class SkillTreeRuntimeState
    {
        private readonly Dictionary<SkillNodeData, int> ranks = new Dictionary<SkillNodeData, int>();
        private readonly HashSet<string> unlockedBookIds = new HashSet<string>();

        public double Grace { get; private set; }

        /// <summary>Stand-in for PrestigeSystem.ResetPrestigeCount &gt; 0 until this tree is wired
        /// to the real prestige system - settable so the UI can be tested in both states.</summary>
        public bool HasResetPrestiged { get; set; }

        public SkillTreeRuntimeState(double startingGrace)
        {
            Grace = startingGrace;
        }

        public int GetRank(SkillNodeData node) => node != null && ranks.TryGetValue(node, out var r) ? r : 0;

        public bool IsMaxed(SkillNodeData node) => node != null && GetRank(node) >= node.maxRank;

        public bool IsBookUnlocked(string bookResourceId) =>
            string.IsNullOrEmpty(bookResourceId) || unlockedBookIds.Contains(bookResourceId);

        /// <summary>The gates a node carries beyond its rank-prerequisite graph - reset-prestige
        /// gating and book-active gating. Kept separate from SkillNodeData.PrerequisitesSatisfied
        /// (a pure graph question the asset itself can answer) since these two checks need actual
        /// runtime save state to resolve.</summary>
        public bool ExtraGatesSatisfied(SkillNodeData node)
        {
            if (node == null) return false;
            if (node.requiresResetPrestige && !HasResetPrestiged) return false;
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
            Grace -= node.GetNextCost(GetRank(node));
            ranks[node] = GetRank(node) + 1;
            return true;
        }

        public bool TryUnlockBook(BookMasteryData book)
        {
            if (book == null || unlockedBookIds.Contains(book.bookResourceId)) return false;
            double cost = book.GetUnlockCost();
            if (Grace < cost) return false;
            Grace -= cost;
            unlockedBookIds.Add(book.bookResourceId);
            return true;
        }

        /// <summary>Debug/testing hook for Phase 2 (grant Grace without a real economy behind it) -
        /// remove or replace once this state is wired to the real wallet.</summary>
        public void AddGrace(double amount) => Grace += amount;
    }
}
