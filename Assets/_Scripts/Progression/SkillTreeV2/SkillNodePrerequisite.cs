using System;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// One entry in a SkillNodeData's prerequisite list. Unlike the existing (shipped)
    /// PrestigeSkillNode.SkillPrerequisite, which references its target by string id because the
    /// old tree is generated at runtime rather than authored as assets, this references the
    /// prerequisite SkillNodeData asset directly - a designer drags the node into the slot instead
    /// of typing an id, and a renamed/reorganized asset can't silently break a dangling string
    /// reference.
    ///
    /// A node's prerequisites list is always AND logic: every entry must have its required rank
    /// bought before the node becomes buyable. This is also how "multi-prerequisite" nodes work -
    /// Convergence needing all 8 branch capstones simultaneously is just a SkillNodeData whose
    /// prerequisites list has 8 entries, one per capstone, each with rankRequired equal to that
    /// capstone's own maxRank. No separate "multi-requirement" node type is needed.
    /// </summary>
    [Serializable]
    public class SkillNodePrerequisite
    {
        public SkillNodeData node;

        /// <summary>Rank the referenced node must have reached. Almost always equal to that node's
        /// own maxRank (i.e. "fully maxed"), but left as its own field rather than a bool so a
        /// future design can require a partial rank (e.g. "rank 2 of a 5-rank node") without a
        /// data-shape change.</summary>
        public int rankRequired = 1;
    }
}
