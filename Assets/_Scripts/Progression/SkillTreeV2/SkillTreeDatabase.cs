using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// Single entry-point asset the UI layer (Phase 2+) will load to get the whole redesigned tree:
    /// the Core node, the 8 economy branches, Convergence, and every book's Mastery data. Phase 1
    /// data layer for the OT Skill Tree redesign (task #195) - "before we draw anything on the
    /// screen, we need the database," per the user's own framing. Holds references only; it does
    /// not track owned ranks or spent Grace itself (that's runtime save state, which belongs in a
    /// system class once Phase 2 exists, the same separation PrestigeSkillSystem already keeps from
    /// PrestigeSkillTreeConfig in the shipped tree).
    /// </summary>
    [CreateAssetMenu(fileName = "SkillTreeDatabase", menuName = "Clicker Genesis/Skill Tree/Skill Tree Database", order = 2)]
    public class SkillTreeDatabase : ScriptableObject
    {
        [Header("Core")]
        public SkillNodeData core;

        [Header("Economy Branches")]
        /// <summary>Every economy-branch node across all 8 branches (Ink Flow, Steady Hand,
        /// Overseer's Wisdom, Illuminated Pages, Scribe's Diligence, Grace of Memorization, Swift
        /// Unlock, Manager's Calling), flat rather than grouped by branch - branch identity lives on
        /// each node's own branchCategory field, and the actual chain order/shape is fully encoded
        /// by each node's prerequisites, so a flat list doesn't lose any structure.</summary>
        public List<SkillNodeData> economyNodes = new List<SkillNodeData>();

        [Header("Convergence")]
        /// <summary>The single node whose prerequisites list names all 8 branch capstones - see
        /// SkillNodePrerequisite's doc comment for why that's sufficient to express "requires all 8
        /// capstones simultaneously" without a dedicated multi-requirement node type.</summary>
        public SkillNodeData convergence;

        [Header("Books")]
        public List<BookMasteryData> books = new List<BookMasteryData>();

        /// <summary>Looks up a book's Mastery data by its Resources/Verses resource id - the same
        /// lookup key GameLoopController already uses elsewhere for a book, so callers don't need a
        /// second id scheme.</summary>
        public BookMasteryData FindBook(string bookResourceId)
        {
            if (books == null || string.IsNullOrEmpty(bookResourceId)) return null;
            foreach (var b in books)
                if (b != null && b.bookResourceId == bookResourceId) return b;
            return null;
        }

        /// <summary>Every node in the tree - core, economy branches, Convergence, and every
        /// authored book's curated Mastery nodes - flattened for validation/editor tooling (e.g.
        /// "does anything have a null prerequisite reference"). Not intended for hot-path runtime
        /// use.</summary>
        public IEnumerable<SkillNodeData> GetAllNodes()
        {
            if (core != null) yield return core;
            if (economyNodes != null)
                foreach (var n in economyNodes)
                    if (n != null) yield return n;
            if (convergence != null) yield return convergence;
            if (books != null)
                foreach (var book in books)
                {
                    if (book?.curatedNodes == null) continue;
                    foreach (var n in book.curatedNodes)
                        if (n != null) yield return n;
                }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only sanity check: flags any node whose prerequisite list has a null
        /// slot (an unassigned reference left mid-authoring) or that's unreachable from Core given
        /// the currently-wired prerequisite graph. Returns a human-readable problem list, empty if
        /// clean. Not part of any runtime code path.</summary>
        public List<string> ValidateGraph()
        {
            var problems = new List<string>();
            var all = new List<SkillNodeData>(GetAllNodes());
            var reachable = new HashSet<SkillNodeData>();
            if (core != null) reachable.Add(core);

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var node in all)
                {
                    if (reachable.Contains(node)) continue;
                    if (node.prerequisites == null || node.prerequisites.Count == 0)
                    {
                        reachable.Add(node); changed = true; continue;
                    }
                    bool allPrereqsReachable = true;
                    foreach (var p in node.prerequisites)
                    {
                        if (p.node == null) { problems.Add($"{node.name}: has a null prerequisite slot."); continue; }
                        if (!reachable.Contains(p.node)) { allPrereqsReachable = false; break; }
                    }
                    if (allPrereqsReachable) { reachable.Add(node); changed = true; }
                }
            }

            foreach (var node in all)
                if (!reachable.Contains(node))
                    problems.Add($"{node.name}: not reachable from Core (a prerequisite is itself unreachable).");

            return problems;
        }
#endif
    }
}
