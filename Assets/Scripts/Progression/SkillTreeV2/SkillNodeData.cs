using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// One node in the redesigned (per-book-branch) Grace skill tree, authored as its own asset
    /// rather than a plain C# entry in a runtime-generated list. Phase 1 data layer for the OT
    /// Skill Tree redesign (task #195, spec'd against the approved constellation mockup) - this
    /// intentionally does NOT touch or replace the shipped PrestigeSkillNode/PrestigeSkillTreeConfig
    /// system, which still powers the live game; the two coexist under separate namespaces
    /// (ClickerGenesis.Progression vs ClickerGenesis.Progression.SkillTreeV2) until this new tree is
    /// reviewed, built out, and ready to cut over.
    ///
    /// Reuses SkillEffectType and SkillNodeShape from the existing PrestigeSkillNode.cs rather than
    /// redeclaring them - the effect semantics (what a node's rank bonus actually plugs into on
    /// GameLoopController) and the visual shape vocabulary are identical between both trees, so
    /// duplicating the enums would just create two definitions that could silently drift apart.
    ///
    /// One asset per node (rather than one big config listing every node, like the old tree) is a
    /// deliberate choice matching the mockup's real per-book scale: a curated 19-node Genesis
    /// Mastery sub-tree needs 19 individually authorable, individually cross-referenceable assets,
    /// not 19 entries hand-typed into a shared list.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillNode", menuName = "Clicker Genesis/Skill Tree/Skill Node", order = 0)]
    public class SkillNodeData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Stable save-key (2026-08-09, real-economy integration). Optional in the
        /// Inspector - falls back to displayName via the Id property below, since every dummy/test
        /// node authored so far already has a unique displayName and forcing this field on every
        /// existing asset isn't worth the busywork. Set it explicitly once a node's displayName
        /// might ever be renamed for content/flavor reasons without wanting to orphan players'
        /// saved ranks.</summary>
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;

        /// <summary>The key SkillTreeRuntimeState's save export/import actually uses.</summary>
        public string Id => string.IsNullOrEmpty(id) ? displayName : id;

        /// <summary>Freeform grouping label for editor/UI organization - e.g. "Ink Flow",
        /// "Book Progression", "Genesis Mastery". Not used for gameplay logic (prerequisites are
        /// what actually gate a node), purely a category tag so a designer or list view can filter
        /// a large tree without reading every node's prerequisites.</summary>
        public string branchCategory;

        [Header("Cost")]
        public double baseCost;

        /// <summary>Multiplier applied per rank already owned - GetNextCost = baseCost *
        /// costMultiplier^ownedRank. The real economy-branch value is 2.0 (confirmed against
        /// Grace-Skill-Tree.html); Book Progression-style single-rank nodes use 1.0 (no per-rank
        /// growth, since there's only ever one rank to buy).</summary>
        public double costMultiplier = 1.0;

        public int maxRank = 1;

        [Header("Effect")]
        public SkillEffectType effectType;

        /// <summary>Bonus granted per rank, in the units GameLoopController expects for that effect
        /// type (e.g. 0.02 = +2%). Unused when effectType == BookUnlock.</summary>
        public double effectPerRank;

        [Header("Prerequisites (AND logic)")]
        /// <summary>Every entry must be satisfied before this node is buyable. Empty = a root node
        /// (Core, or a book's own Mastery hub). A node needing several other skills at once - the
        /// canonical example being Convergence, which requires all 8 branch capstones maxed
        /// simultaneously - is expressed by simply listing all of them here; there is no separate
        /// "multi-requirement" node type.</summary>
        public List<SkillNodePrerequisite> prerequisites = new List<SkillNodePrerequisite>();

        [Header("Gating beyond rank prerequisites")]
        /// <summary>Only set when effectType == BookUnlock - the book's Resources/Verses/{id}.json
        /// resource id this node grants access to. Empty for every other effect type and for
        /// generic "player picks a book at purchase time" slots (those resolve the chosen book at
        /// runtime instead of baking it into the asset).</summary>
        public string unlockBookResourceId;

        /// <summary>When set, this node also requires the named book to already be unlocked/active
        /// (checked against GameLoopController's book-unlock state) on top of its rank
        /// prerequisites above - e.g. every node inside a book's own Mastery sub-tree requires that
        /// book specifically, even though its in-tree prerequisite is just "the previous Mastery
        /// node in this same book's chain."</summary>
        public string requiresBookResourceId;

        /// <summary>Mirrors the shipped tree's reset-gating: this node cannot be bought until the
        /// player has performed at least one opt-in Reset-Prestige, regardless of whether its rank
        /// prerequisites are otherwise satisfied. Reserved for capstones and Convergence itself.</summary>
        public bool requiresResetPrestige;

        [Header("Visual")]
        public SkillNodeShape shape = SkillNodeShape.Circle;

        /// <summary>Real icon art (2026-08-09) - SkillNodeUI draws this over the shape when the
        /// node is Owned/Frontier. Null is fine (falls back to the flat shape alone), matching the
        /// existing project rule of leaving a node icon-less rather than forcing a mismatched
        /// placeholder onto it.</summary>
        public Sprite icon;

        /// <summary>Accent color for this node's rendered state (owned/frontier) - a plain data
        /// hint for the UI layer to consume, not a rendering implementation itself. Left at a
        /// neutral default so unset nodes don't render pure black.</summary>
        public Color accentColor = new Color(0.85f, 0.75f, 0.42f);

        /// <summary>True for branch-ending nodes (Convergence's 8 prerequisites) - purely a filter
        /// flag for editor tooling/validation, since the real "is this a capstone" behavior already
        /// falls out of the prerequisite graph (a capstone is just whatever node Convergence lists
        /// as a prerequisite).</summary>
        public bool isCapstone;

        /// <summary>Cost to purchase the NEXT rank, given how many ranks are already owned. Pure
        /// data-layer math (no save-state lookup) - the runtime system owns actual rank tracking;
        /// this just answers "what would rank N+1 cost" for a given current rank.</summary>
        public double GetNextCost(int currentRank)
        {
            return baseCost * System.Math.Pow(costMultiplier, currentRank);
        }

        /// <summary>Whether every prerequisite entry is satisfied, given a rank lookup delegate -
        /// deliberately takes a lookup function rather than reading any save/runtime state itself,
        /// since this asset has no notion of "the current save" and shouldn't need one to answer a
        /// pure graph question.</summary>
        public bool PrerequisitesSatisfied(System.Func<SkillNodeData, int> getOwnedRank)
        {
            if (prerequisites == null || prerequisites.Count == 0) return true;
            foreach (var prereq in prerequisites)
            {
                if (prereq.node == null) continue; // tolerate an unassigned slot mid-authoring
                if (getOwnedRank(prereq.node) < prereq.rankRequired) return false;
            }
            return true;
        }
    }
}
