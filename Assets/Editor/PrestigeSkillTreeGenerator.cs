using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClickerGenesis.Progression;

namespace ClickerGenesis.EditorTools
{
    /// <summary>
    /// One-off generator for the Grace skill tree data asset - procedural, not hand-authored row
    /// by row in the Inspector, same pattern used for GenesisScribes.asset. Re-running this
    /// overwrites the existing asset with freshly-generated nodes (ranks are stored on the
    /// runtime PrestigeSkillSystem, not on this config, so regenerating the config never touches
    /// a save - there's no save system yet anyway).
    /// </summary>
    public static class PrestigeSkillTreeGenerator
    {
        private struct BranchSpec
        {
            public string name;
            public string flavor;
            public string capstoneName;
            public SkillEffectType effectType;
            public int[] ranks;
            public double[] perRankEffect;
            public double[] baseCost;
        }

        [MenuItem("Clicker Genesis/Generate Prestige Skill Tree")]
        public static void Generate()
        {
            var config = ScriptableObject.CreateInstance<PrestigeSkillTreeConfig>();
            config.branchOrder = new List<string>();
            config.nodes = new List<PrestigeSkillNode>();

            // Shared 8-step shape per economy branch: 3 filler nodes (rank 1), 2 minor investments
            // (rank 3), 2 major investments (rank 5), 1 capstone (rank 1, bigger bonus). Each step
            // requires the previous step maxed before it unlocks - the "3-4 upgrades before the
            // next one unlocks" behavior from the design brief.
            int[] shape = { 1, 1, 1, 3, 3, 5, 5, 1 };

            // Cost curve retuned 2026-08-06 (user's explicit ask: "the amount of grace they're
            // able to spend per item also goes up by a significant amount between each skill they
            // unlock... so that they're required to reset"). Base costs bumped ~1.5x and the
            // per-rank growth rate (see BuildBranch's costGrowthPerRank) raised from 1.6 to 2.0 -
            // together these compound hard on the 5-rank nodes (roughly 3x the old total cost by
            // the time a branch reaches its capstone), pushing toward needing a Reset's bigger
            // Grace payout instead of casual Free-prestige grinding. Same "placeholder pending
            // playtesting" rule as every other numeric constant in this project.
            var branches = new BranchSpec[]
            {
                new BranchSpec { name = "Ink Flow", flavor = "Ink", capstoneName = "River of Living Ink",
                    effectType = SkillEffectType.IncomeMultiplier, ranks = shape,
                    perRankEffect = new double[] { 0.02, 0.02, 0.02, 0.03, 0.03, 0.025, 0.025, 0.15 },
                    baseCost = new double[] { 8, 12, 18, 23, 38, 60, 105, 375 } },

                new BranchSpec { name = "Steady Hand", flavor = "Tap", capstoneName = "Scribe's Unshaking Hand",
                    effectType = SkillEffectType.ClickPowerMultiplier, ranks = shape,
                    perRankEffect = new double[] { 0.025, 0.025, 0.025, 0.03, 0.03, 0.025, 0.025, 0.15 },
                    baseCost = new double[] { 8, 12, 18, 23, 38, 60, 105, 375 } },

                new BranchSpec { name = "Overseer's Wisdom", flavor = "Manager", capstoneName = "Joseph's Full Storehouse",
                    effectType = SkillEffectType.ManagerBonusBoost, ranks = shape,
                    perRankEffect = new double[] { 0.02, 0.02, 0.02, 0.025, 0.025, 0.02, 0.02, 0.10 },
                    baseCost = new double[] { 15, 23, 33, 45, 68, 98, 135, 450 } },

                new BranchSpec { name = "Illuminated Pages", flavor = "Progress", capstoneName = "Marginalia of the Faithful",
                    effectType = SkillEffectType.ProgressMultiplierBoost, ranks = shape,
                    perRankEffect = new double[] { 0.02, 0.02, 0.02, 0.03, 0.03, 0.025, 0.025, 0.15 },
                    baseCost = new double[] { 9, 15, 23, 30, 45, 68, 98, 330 } },

                new BranchSpec { name = "Scribe's Diligence", flavor = "Milestone", capstoneName = "Tireless Copyist",
                    effectType = SkillEffectType.ScribeMilestoneBoost, ranks = shape,
                    perRankEffect = new double[] { 0.02, 0.02, 0.02, 0.025, 0.025, 0.02, 0.02, 0.10 },
                    baseCost = new double[] { 9, 15, 23, 30, 45, 68, 98, 330 } },

                new BranchSpec { name = "Grace of Memorization", flavor = "Grace", capstoneName = "Perfect Recall",
                    effectType = SkillEffectType.GraceGainBonus, ranks = shape,
                    perRankEffect = new double[] { 0.02, 0.02, 0.02, 0.03, 0.03, 0.025, 0.025, 0.15 },
                    baseCost = new double[] { 12, 18, 27, 38, 53, 83, 128, 450 } },

                new BranchSpec { name = "Swift Unlock", flavor = "Discount", capstoneName = "Open Door",
                    effectType = SkillEffectType.PricingDiscount, ranks = shape,
                    perRankEffect = new double[] { 0.015, 0.015, 0.015, 0.02, 0.02, 0.015, 0.015, 0.08 },
                    baseCost = new double[] { 9, 15, 23, 30, 45, 68, 98, 330 } },

                new BranchSpec { name = "Manager's Calling", flavor = "Unlock", capstoneName = "Called Before Their Time",
                    effectType = SkillEffectType.ManagerUnlockLevelDiscount, ranks = shape,
                    perRankEffect = new double[] { 1, 1, 1, 1, 1, 1, 1, 3 },
                    baseCost = new double[] { 15, 23, 30, 45, 68, 105, 150, 525 } },
            };

            // Central hub node (2026-08-06, user's explicit correction: "you have to buy the core
            // in the middle first, which means you need to make all of these skill trees connect
            // to the one in the middle" - not merely "any Grace unlocks any branch root"). Every
            // branch root below - all 8 economy branches AND Book Progression's first node - now
            // requires Core at rank 1, replacing the old bare `prerequisiteId = null` roots.
            config.nodes.Add(new PrestigeSkillNode
            {
                id = "Core",
                displayName = "Grace Awakened",
                description = "Awakens your capacity to draw on Grace. Every skill on this tree - every branch, and Book Progression - requires this first.",
                branch = "Core",
                prerequisites = new List<SkillPrerequisite>(),
                shape = SkillNodeShape.Star,
                maxRank = 1,
                baseCost = 8,
                costGrowthPerRank = 1.0,
                effectType = SkillEffectType.IncomeMultiplier,
                effectPerRank = 0,
                unlockBookResourceId = "",
                unlockBookDisplayName = "",
                isCapstone = false,
            });

            foreach (var b in branches)
            {
                config.branchOrder.Add(b.name);
                BuildBranch(config, b);
            }

            // Book Progression's nodes are NOT generated here - they're built at runtime by
            // BookProgressionTreeBuilder, anchored to whichever book the player actually started
            // in (2026-08-08 redesign). This asset only ever holds Core + the 8 economy branches;
            // GameLoopController merges in the runtime book nodes on top of a clone of this asset
            // once a save's starting book is known. branchOrder still lists "Book Progression" so
            // the tree UI groups those runtime nodes correctly once merged in.
            config.branchOrder.Add("Book Progression");

            // Convergence capstone (2026-08-08) - the concrete demonstration of the new
            // multi-prerequisite feature: requires ALL 8 branch capstones maxed at once, not a
            // single chain step. Sits conceptually "above" every branch, its own reward for having
            // fully invested in every economy branch.
            BuildConvergenceNode(config, branches);

            const string path = "Assets/Config/PrestigeSkillTreeConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrestigeSkillTreeConfig>(path);
            if (existing != null)
            {
                existing.branchOrder = config.branchOrder;
                existing.nodes = config.nodes;
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(config, path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated PrestigeSkillTreeConfig with {config.nodes.Count} nodes across {config.branchOrder.Count} branches.");
        }

        private static void BuildBranch(PrestigeSkillTreeConfig config, BranchSpec b)
        {
            // Every branch's first node now requires Core at rank 1 instead of being a bare,
            // no-prerequisite root (2026-08-06 hub redesign) - prevId/prevMaxRank start seeded
            // with Core's own id/maxRank rather than null/0.
            string prevId = "Core";
            int prevMaxRank = 1;
            for (int i = 0; i < b.ranks.Length; i++)
            {
                bool isCapstone = i == b.ranks.Length - 1;
                string id = $"{b.flavor}_{i}";
                string displayName = isCapstone ? b.capstoneName : $"{b.name} {ToRoman(i + 1)}";

                config.nodes.Add(new PrestigeSkillNode
                {
                    id = id,
                    displayName = displayName,
                    description = DescribeEffect(b.effectType, b.perRankEffect[i]),
                    branch = b.name,
                    prerequisites = new List<SkillPrerequisite> { new SkillPrerequisite(prevId, prevMaxRank) },
                    // Capstones get their own shape (Hexagon) so they read as a different tier of
                    // node from the regular rank-chain steps (Circle) at a glance (2026-08-08).
                    shape = isCapstone ? SkillNodeShape.Hexagon : SkillNodeShape.Circle,
                    maxRank = b.ranks[i],
                    baseCost = b.baseCost[i],
                    costGrowthPerRank = 2.0,
                    effectType = b.effectType,
                    effectPerRank = b.perRankEffect[i],
                    unlockBookResourceId = "",
                    unlockBookDisplayName = "",
                    isCapstone = isCapstone,
                    // Every branch's capstone requires having done at least one Reset-Prestige
                    // (2026-08-06, user's explicit ask) - the biggest single-node payoffs are
                    // reserved for players who've actually reset, not just accumulated Grace via
                    // Free prestiges.
                    requiresResetPrestige = isCapstone,
                });

                prevId = id;
                prevMaxRank = b.ranks[i];
            }
        }

        /// <summary>The tree's one true multi-prerequisite node (2026-08-08) - requires every
        /// branch's capstone maxed simultaneously, not a single chain step. Demonstrates the new
        /// AND-prerequisite-list mechanism for real rather than leaving it unused scaffolding.</summary>
        private static void BuildConvergenceNode(PrestigeSkillTreeConfig config, BranchSpec[] branches)
        {
            var prereqs = new List<SkillPrerequisite>();
            foreach (var b in branches)
            {
                string capstoneId = $"{b.flavor}_{b.ranks.Length - 1}";
                int capstoneMaxRank = b.ranks[b.ranks.Length - 1];
                prereqs.Add(new SkillPrerequisite(capstoneId, capstoneMaxRank));
            }

            config.nodes.Add(new PrestigeSkillNode
            {
                id = "Convergence",
                displayName = "Grace Made Perfect",
                description = "Requires every branch's capstone fully mastered. +25% total Ink/sec, on top of every other bonus.",
                branch = "Core",
                prerequisites = prereqs,
                shape = SkillNodeShape.Triangle,
                maxRank = 1,
                baseCost = 5000,
                costGrowthPerRank = 1.0,
                effectType = SkillEffectType.IncomeMultiplier,
                effectPerRank = 0.25,
                unlockBookResourceId = "",
                unlockBookDisplayName = "",
                isCapstone = true,
                requiresResetPrestige = true,
            });
        }

        private static string DescribeEffect(SkillEffectType type, double perRank)
        {
            switch (type)
            {
                case SkillEffectType.IncomeMultiplier: return $"+{perRank * 100:0.#}% total Ink/sec per rank.";
                case SkillEffectType.ClickPowerMultiplier: return $"+{perRank * 100:0.#}% tap value per rank.";
                case SkillEffectType.ManagerBonusBoost: return $"+{perRank * 100:0.#}% manager bonus per rank.";
                case SkillEffectType.ProgressMultiplierBoost: return $"+{perRank * 100:0.#}% progress multiplier per rank.";
                case SkillEffectType.ScribeMilestoneBoost: return $"+{perRank * 100:0.#}% scribe milestone bonus per rank.";
                case SkillEffectType.GraceGainBonus: return $"+{perRank * 100:0.#}% Grace earned per prestige, per rank.";
                case SkillEffectType.PricingDiscount: return $"-{perRank * 100:0.#}% verse/chapter Ink cost per rank.";
                case SkillEffectType.ManagerUnlockLevelDiscount: return $"-{perRank:0} manager unlock level requirement per rank.";
                default: return "";
            }
        }

        private static string ToRoman(int n)
        {
            string[] tens = { "", "X", "XX", "XXX" };
            string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
            return tens[n / 10] + ones[n % 10];
        }
    }
}
