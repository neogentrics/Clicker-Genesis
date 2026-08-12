using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// Pure position math - no MonoBehaviour, no scene dependency - so it can be unit-tested and
    /// reused for both the main economy web and every book's isolated Mastery sub-tree with the
    /// same recursive algorithm. The graph itself (SkillNodeData.prerequisites) is the only source
    /// of truth for shape: a node with several children fans them out; a straight chain just draws
    /// a ray. No separate "spoke layout" special-case is needed for real curated content - only
    /// the procedural fallback (for books without curated nodes yet) has to manufacture children to
    /// get a similar sprawling look.
    /// </summary>
    public static class SkillTreeLayoutGenerator
    {
        public class LayoutResult
        {
            public readonly Dictionary<SkillNodeData, Vector2> Positions = new Dictionary<SkillNodeData, Vector2>();
            public Rect Bounds;
        }

        private const float RadiusStep = 110f;
        private const float BoundsPadding = 160f;

        /// <summary>Deterministic per-seed RNG (string hash, not Unity's global Random) so the same
        /// database always lays out identically across sessions - a designer reviewing the tree
        /// twice should see the same shape, not a re-shuffled one.</summary>
        public static System.Random SeededRandom(string seed)
        {
            unchecked
            {
                int hash = 17;
                if (seed != null)
                    foreach (char c in seed) hash = hash * 31 + c;
                return new System.Random(hash);
            }
        }

        /// <summary>Builds parent -&gt; children edges for every node whose ONLY prerequisite is
        /// that parent. A node with multiple prerequisites (Convergence, or any future "requires
        /// several skills at once" node) deliberately does not appear as anyone's chain child here -
        /// it's positioned separately as a centroid of its prerequisites once the fan layout below
        /// has already placed them.</summary>
        public static Dictionary<SkillNodeData, List<SkillNodeData>> BuildChildrenMap(IEnumerable<SkillNodeData> allNodes)
        {
            var map = new Dictionary<SkillNodeData, List<SkillNodeData>>();
            foreach (var node in allNodes)
            {
                if (node == null || node.prerequisites == null || node.prerequisites.Count != 1) continue;
                var parent = node.prerequisites[0].node;
                if (parent == null) continue;
                if (!map.TryGetValue(parent, out var list)) map[parent] = list = new List<SkillNodeData>();
                list.Add(node);
            }
            return map;
        }

        /// <summary>Recursively fans a node's children within [startAngle, startAngle+angleSpan]
        /// degrees around the shared origin, stepping outward by RadiusStep per depth level, with a
        /// small deterministic jitter so chains don't read as perfectly rigid spokes.</summary>
        public static void LayoutSubtree(SkillNodeData root, Vector2 rootPos, float startAngle, float angleSpan,
            Dictionary<SkillNodeData, List<SkillNodeData>> childrenMap, Dictionary<SkillNodeData, Vector2> positions,
            int depth = 1)
        {
            if (!childrenMap.TryGetValue(root, out var children) || children.Count == 0) return;

            float step = angleSpan / children.Count;
            var rnd = SeededRandom(root.name + "_" + depth);
            for (int i = 0; i < children.Count; i++)
            {
                float sliceStart = startAngle + step * i;
                float angle = sliceStart + step * 0.5f + (float)(rnd.NextDouble() - 0.5) * step * 0.3f;
                float radius = RadiusStep * depth + (float)(rnd.NextDouble() - 0.5) * 24f;
                var pos = rootPos + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                positions[children[i]] = pos;
                LayoutSubtree(children[i], pos, sliceStart, step, childrenMap, positions, depth + 1);
            }
        }

        /// <summary>Core + all 8 economy branches + Convergence. Convergence is the centroid of its
        /// own prerequisite (capstone) positions, pushed further outward - it's never anyone's
        /// single-parent child so LayoutSubtree alone would never place it.</summary>
        public static LayoutResult BuildEconomyLayout(SkillTreeDatabase db)
        {
            var result = new LayoutResult();
            if (db == null || db.core == null) return result;

            result.Positions[db.core] = Vector2.zero;

            var childrenMap = BuildChildrenMap(db.economyNodes ?? new List<SkillNodeData>());
            var branchRoots = childrenMap.TryGetValue(db.core, out var roots) ? roots : new List<SkillNodeData>();

            float step = 360f / Mathf.Max(1, branchRoots.Count);
            for (int i = 0; i < branchRoots.Count; i++)
            {
                float sliceStart = -90f + step * i;
                var rnd = SeededRandom(branchRoots[i].branchCategory ?? branchRoots[i].name);
                float angle = sliceStart + step * 0.5f + (float)(rnd.NextDouble() - 0.5) * step * 0.3f;
                var pos = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * RadiusStep;
                result.Positions[branchRoots[i]] = pos;
                LayoutSubtree(branchRoots[i], pos, sliceStart, step, childrenMap, result.Positions, 2);
            }

            if (db.convergence != null)
            {
                Vector2 sum = Vector2.zero;
                int count = 0;
                if (db.convergence.prerequisites != null)
                {
                    foreach (var p in db.convergence.prerequisites)
                        if (p.node != null && result.Positions.TryGetValue(p.node, out var pos)) { sum += pos; count++; }
                }
                Vector2 convergencePos = count > 0
                    ? (sum / count).normalized * ((sum / count).magnitude + RadiusStep * 1.6f)
                    : new Vector2(0f, -RadiusStep * 4f); // fallback if no capstones are wired yet
                result.Positions[db.convergence] = convergencePos;
            }

            result.Bounds = ComputeBounds(result.Positions.Values);
            return result;
        }

        /// <summary>Lays out an isolated book Mastery sub-tree in its own local coordinate space
        /// (0,0 = that sub-tree's hub, unrelated to the main web's coordinates). Works identically
        /// whether nodes came from BookMasteryData.curatedNodes (real, hand-authored branching) or
        /// GenerateProceduralMasteryNodes below (a manufactured chain/spoke shape) - the algorithm
        /// doesn't know or care which.</summary>
        public static LayoutResult BuildBookMasteryLayout(BookMasteryData book, List<SkillNodeData> nodes)
        {
            var result = new LayoutResult();
            if (nodes == null || nodes.Count == 0) return result;

            var childrenMap = BuildChildrenMap(nodes);
            var roots = new List<SkillNodeData>();
            foreach (var n in nodes)
                if (n.prerequisites == null || n.prerequisites.Count == 0) roots.Add(n);

            float step = 360f / Mathf.Max(1, roots.Count);
            for (int i = 0; i < roots.Count; i++)
            {
                float sliceStart = -90f + step * i;
                var rnd = SeededRandom((book?.bookResourceId ?? "book") + "_" + roots[i].name);
                float angle = sliceStart + step * 0.5f + (float)(rnd.NextDouble() - 0.5) * step * 0.3f;
                var pos = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * RadiusStep;
                result.Positions[roots[i]] = pos;
                LayoutSubtree(roots[i], pos, sliceStart, step, childrenMap, result.Positions, 2);
            }

            result.Bounds = ComputeBounds(result.Positions.Values);
            return result;
        }

        /// <summary>Generates transient, non-asset SkillNodeData instances (ScriptableObject.
        /// CreateInstance - the same runtime-node-generation technique the shipped tree already
        /// uses in PostConvergenceTreeBuilder/BookProgressionTreeBuilder, not a new pattern) sized
        /// to BookMasteryData.GetEffectiveNodeCount(), for a book that hasn't had its real Mastery
        /// content curated yet. These are placeholder nodes only, grounded in the same clicker
        /// economy terms (Ink/sec bonus, real cost growth) as everything else - never saved to
        /// disk, and replaced outright the moment a curated node list is authored for that book.</summary>
        public static List<SkillNodeData> GenerateProceduralMasteryNodes(BookMasteryData book)
        {
            var nodes = new List<SkillNodeData>();
            if (book == null) return nodes;

            int count = book.GetEffectiveNodeCount();
            var tier = book.GetSizeTier();
            int spokeCount = tier == BookMasteryData.SizeTier.Short ? 1 : Mathf.Clamp(Mathf.RoundToInt(count / 5f), 3, 5);
            int perSpoke = Mathf.CeilToInt((float)count / spokeCount);

            var rnd = SeededRandom(book.bookResourceId + "_cost");
            int made = 0;
            for (int s = 0; s < spokeCount && made < count; s++)
            {
                SkillNodeData prev = null;
                for (int k = 0; k < perSpoke && made < count; k++, made++)
                {
                    var n = ScriptableObject.CreateInstance<SkillNodeData>();
                    n.name = $"{book.bookResourceId}_mastery_{made}";
                    n.displayName = $"{book.displayName} Mastery {made + 1}";
                    n.branchCategory = $"{book.displayName} Mastery";
                    n.requiresBookResourceId = book.bookResourceId;
                    n.maxRank = 1;
                    n.baseCost = System.Math.Round((30 + rnd.NextDouble() * 40) * System.Math.Pow(1.17, made));
                    n.costMultiplier = 1.0;
                    n.effectType = SkillEffectType.IncomeMultiplier;
                    n.effectPerRank = 0.01;
                    n.shape = SkillNodeShape.Circle;
                    n.accentColor = new Color(0.79f, 0.56f, 0.84f);
                    if (prev != null)
                        n.prerequisites.Add(new SkillNodePrerequisite { node = prev, rankRequired = 1 });
                    nodes.Add(n);
                    prev = n;
                }
            }
            return nodes;
        }

        private static Rect ComputeBounds(IEnumerable<Vector2> points)
        {
            float minX = 0, maxX = 0, minY = 0, maxY = 0;
            bool any = false;
            foreach (var p in points)
            {
                if (!any) { minX = maxX = p.x; minY = maxY = p.y; any = true; continue; }
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            return new Rect(minX - BoundsPadding, minY - BoundsPadding,
                (maxX - minX) + BoundsPadding * 2f, (maxY - minY) + BoundsPadding * 2f);
        }
    }
}
