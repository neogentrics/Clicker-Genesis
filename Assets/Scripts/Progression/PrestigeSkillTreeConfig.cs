using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Progression
{
    /// <summary>Data-driven Grace skill tree - a flat list of nodes, grouped by their `branch`
    /// string field rather than nested Unity objects (simplest structure that supports ~100
    /// entries without an asset-per-node). Generated procedurally by an editor script rather than
    /// hand-authored in the Inspector one row at a time - see GameLoopController's Notion/CLAUDE.md
    /// notes for the branch design.</summary>
    [CreateAssetMenu(fileName = "PrestigeSkillTreeConfig", menuName = "Clicker Genesis/Progression/Prestige Skill Tree Config")]
    public class PrestigeSkillTreeConfig : ScriptableObject
    {
        public List<string> branchOrder = new List<string>();
        public List<PrestigeSkillNode> nodes = new List<PrestigeSkillNode>();

        public PrestigeSkillNode FindNode(string id)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].id == id) return nodes[i];
            return null;
        }
    }
}
