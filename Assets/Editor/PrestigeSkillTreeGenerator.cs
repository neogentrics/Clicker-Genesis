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
                prerequisiteId = null,
                prerequisiteRankRequired = 0,
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

            config.branchOrder.Add("Book Progression");
            BuildBookBranch(config);

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
                    prerequisiteId = prevId,
                    prerequisiteRankRequired = prevMaxRank,
                    maxRank = b.ranks[i],
                    baseCost = b.baseCost[i],
                    costGrowthPerRank = 2.0,
                    effectType = b.effectType,
                    effectPerRank = b.perRankEffect[i],
                    unlockBookResourceId = "",
                    unlockBookDisplayName = "",
                    isCapstone = isCapstone,
                });

                prevId = id;
                prevMaxRank = b.ranks[i];
            }
        }

        // Canonical KJV Old Testament order, excluding Genesis (the always-free starting book) -
        // resource ids match the files already sitting in Assets/Resources/Verses/.
        private static readonly (string id, string name)[] RemainingOldTestament =
        {
            ("exodus_2", "Exodus"), ("leviticus_3", "Leviticus"), ("numbers_4", "Numbers"),
            ("deuteronomy_5", "Deuteronomy"), ("joshua_6", "Joshua"), ("judges_7", "Judges"),
            ("ruth_8", "Ruth"), ("1samuel_9", "1 Samuel"), ("2samuel_10", "2 Samuel"),
            ("1kings_11", "1 Kings"), ("2kings_12", "2 Kings"), ("1chronicles_13", "1 Chronicles"),
            ("2chronicles_14", "2 Chronicles"), ("ezra_15", "Ezra"), ("nehemiah_16", "Nehemiah"),
            ("esther_17", "Esther"), ("job_18", "Job"), ("psalms_19", "Psalms"),
            ("proverbs_20", "Proverbs"), ("ecclesiastes_21", "Ecclesiastes"), ("songofsolomon_22", "Song of Solomon"),
            ("isaiah_23", "Isaiah"), ("jeremiah_24", "Jeremiah"), ("lamentations_25", "Lamentations"),
            ("ezekiel_26", "Ezekiel"), ("daniel_27", "Daniel"), ("hosea_28", "Hosea"),
            ("joel_29", "Joel"), ("amos_30", "Amos"), ("obadiah_31", "Obadiah"),
            ("jonah_32", "Jonah"), ("micah_33", "Micah"), ("nahum_34", "Nahum"),
            ("habakkuk_35", "Habakkuk"), ("zephaniah_36", "Zephaniah"), ("haggai_37", "Haggai"),
            ("zechariah_38", "Zechariah"), ("malachi_39", "Malachi"),
        };

        private static void BuildBookBranch(PrestigeSkillTreeConfig config)
        {
            // First book node also requires Core at rank 1 (2026-08-06 hub redesign, user
            // confirmed Book Progression is gated same as the economy branches) - prevId seeded
            // with "Core" instead of null makes prerequisiteRankRequired below resolve to 1
            // automatically for Exodus, same mechanism BuildBranch uses.
            string prevId = "Core";
            double cost = 30;
            foreach (var (id, name) in RemainingOldTestament)
            {
                string nodeId = $"Book_{id}";
                config.nodes.Add(new PrestigeSkillNode
                {
                    id = nodeId,
                    displayName = name,
                    description = $"Unlocks {name} for purchase on the Books tab.",
                    branch = "Book Progression",
                    prerequisiteId = prevId,
                    prerequisiteRankRequired = prevId == null ? 0 : 1,
                    maxRank = 1,
                    baseCost = System.Math.Round(cost),
                    costGrowthPerRank = 1.0,
                    effectType = SkillEffectType.BookUnlock,
                    effectPerRank = 0,
                    unlockBookResourceId = id,
                    unlockBookDisplayName = name,
                    isCapstone = false,
                });
                prevId = nodeId;
                cost *= 1.18; // each successive OT book costs a bit more Grace than the last
            }

            // New Testament sits behind a small, steeply-priced gate cluster rather than one node
            // per book - "either an absurd amount of grace or paid for by whatever game store"
            // (2026-08-04, user's own framing). No real payment wired up yet - unlockBookResourceId
            // just needs the Grace-bought path to work; a Store-driven alternative unlock is a
            // separate, later hook into the same IsBookUnlocked check.
            AddNtGate(config, "Gospel Threshold", prevId,
                "matthew_40,mark_41,luke_42,john_43", 2000);
            AddNtGate(config, "Apostolic Path", "Book_NT_Gospel Threshold",
                "acts_44,romans_45,1corinthians_46,2corinthians_47,galatians_48,ephesians_49,philippians_50,colossians_51,1thessalonians_52,2thessalonians_53,1timothy_54,2timothy_55,titus_56,philemon_57,hebrews_58,james_59,1peter_60,2peter_61,1john_62,2john_63,3john_64", 5000);
            AddNtGate(config, "Revelation's Veil", "Book_NT_Apostolic Path",
                "jude_65,revelation_66", 8000);
        }

        private static void AddNtGate(PrestigeSkillTreeConfig config, string displayName, string prereqId, string bookIds, double cost)
        {
            string nodeId = $"Book_NT_{displayName}";
            config.nodes.Add(new PrestigeSkillNode
            {
                id = nodeId,
                displayName = displayName,
                description = $"Unlocks New Testament books: {bookIds.Replace(",", ", ")}.",
                branch = "Book Progression",
                prerequisiteId = prereqId,
                prerequisiteRankRequired = prereqId == null ? 0 : 1,
                maxRank = 1,
                baseCost = cost,
                costGrowthPerRank = 1.0,
                effectType = SkillEffectType.BookUnlock,
                effectPerRank = 0,
                unlockBookResourceId = bookIds,
                unlockBookDisplayName = displayName,
                isCapstone = displayName == "Revelation's Veil",
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
