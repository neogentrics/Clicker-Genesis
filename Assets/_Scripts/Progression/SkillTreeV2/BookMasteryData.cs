using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// One book's Mastery sub-tree entry - the data the Convergence book panel reads to price an
    /// "Unlock New Book" purchase and, once unlocked, to open that book's own isolated Mastery
    /// constellation. Phase 1 data layer for the OT Skill Tree redesign (task #195); see
    /// SkillNodeData's header comment for why this coexists with, rather than replaces, the shipped
    /// tree system.
    ///
    /// Node COUNT is intentionally dynamic per book, not a fixed number, per the approved mockup's
    /// Rule 3: "small books generate short, 3-to-5 node linear branches; massive books generate
    /// sprawling, 15-to-25 node constellations." A book gets a real curated node count once its
    /// curatedNodes list is actually authored (matching the real scribe-tier counts already
    /// established elsewhere in this project - Genesis 19, Judges 19, Exodus 10, etc. - since a
    /// book with a bigger existing content library earns a bigger Mastery tree, not an arbitrary
    /// number); until then, GetEffectiveNodeCount() falls back to a chapter-count-derived estimate
    /// so the UI layer always has a sane number to lay out against, never a hard-coded placeholder.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBookMastery", menuName = "Clicker Genesis/Skill Tree/Book Mastery", order = 1)]
    public class BookMasteryData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>The book's Resources/Verses/{id}.json resource id (e.g. "genesis_1") - the same
        /// identifier GameLoopController/CanonicalBookOrder already use, so this asset can be
        /// looked up by the same key the rest of the game uses for a book, not a second naming
        /// scheme.</summary>
        public string bookResourceId;

        public string displayName;

        /// <summary>Short evocative phrase shown alongside the book name on its Mastery hub node
        /// (e.g. Genesis -> "Creation's Breath", Romans -> "The Just Shall Live"), matching the
        /// concept-art reference's "{Book}: {Theme}" node-title convention. Left blank for books
        /// that haven't had a real thematic pass yet - the UI layer falls back to a generic
        /// "{Book} Mastery" label rather than inventing lore here.</summary>
        public string thematicTitle;

        [Header("Canonical order (display/authoring only - see note)")]
        /// <summary>1-based canonical position, used for sorting the book list in the UI. NOT the
        /// runtime unlock cost source (2026-08-09 correction) - the real design (Grace-Skill-Tree.
        /// html) prices Book Progression as 38 generic, interchangeable slots where cost climbs
        /// with how many books the PLAYER has already bought, not with which book they pick each
        /// time; that logic now lives in SkillTreeRuntimeState.NextBookUnlockCost. GetUnlockCost()
        /// below is kept only as authoring-time reference for "what would this cost if it were
        /// bought in canonical order."</summary>
        public int slotIndex;

        public const double SlotCostBase = 30.0;
        public const double SlotCostGrowth = 1.22;

        /// <summary>The real Book Progression cost formula: round(30 x 1.22^(n-1)). Returns 0 for
        /// slotIndex &lt;= 0 (the always-free starting book).</summary>
        public double GetUnlockCost()
        {
            if (slotIndex <= 0) return 0;
            return System.Math.Round(SlotCostBase * System.Math.Pow(SlotCostGrowth, slotIndex - 1));
        }

        [Header("Mastery sub-tree size")]
        /// <summary>Real chapter count (from kjv_outline.json) - drives the fallback size estimate
        /// below when this book hasn't had a curated sub-tree authored yet.</summary>
        public int chapterCount;

        /// <summary>The book's actual Mastery nodes, once authored. A small book (Ruth, Micah) gets
        /// a handful of SkillNodeData assets forming a short linear chain; a major book (Genesis,
        /// Psalms) gets a much larger set, typically organized into several branching sub-chains off
        /// the book's own hub. Left empty for a book that hasn't been curated yet - see
        /// GetEffectiveNodeCount for what the UI should assume in that case.</summary>
        public List<SkillNodeData> curatedNodes = new List<SkillNodeData>();

        /// <summary>How many Mastery nodes this book's sub-tree should be laid out with - the real
        /// curated count once curatedNodes is populated, otherwise a chapter-count-derived estimate
        /// (clamped 3-25, matching the mockup's small/large book range) so layout code never has to
        /// special-case "no data yet."</summary>
        public int GetEffectiveNodeCount()
        {
            if (curatedNodes != null && curatedNodes.Count > 0) return curatedNodes.Count;
            int estimate = Mathf.RoundToInt(Mathf.Max(1, chapterCount) / 2.2f);
            return Mathf.Clamp(estimate, 3, 25);
        }

        public enum SizeTier { Short, Medium, Sprawling }

        /// <summary>Coarse bucket the UI layer can use to pick a layout strategy (linear chain vs.
        /// multi-spoke constellation) without re-deriving thresholds itself.</summary>
        public SizeTier GetSizeTier()
        {
            int count = GetEffectiveNodeCount();
            if (count <= 5) return SizeTier.Short;
            if (count <= 12) return SizeTier.Medium;
            return SizeTier.Sprawling;
        }
    }
}
