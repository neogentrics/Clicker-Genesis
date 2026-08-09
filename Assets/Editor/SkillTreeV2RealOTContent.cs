using System.Collections.Generic;
using ClickerGenesis.Progression;
using ClickerGenesis.Progression.SkillTreeV2;
using ClickerGenesis.Data;
using UnityEditor;
using UnityEngine;

namespace ClickerGenesis.EditorTools
{
    /// <summary>
    /// Authors the REAL Old Testament Grace Skill Tree content (task #195) - Core, the 8 real
    /// economy branches, Convergence, and real BookMasteryData for all 39 canonical OT books.
    /// Every name/cost/rank/effect number below is transcribed directly from
    /// Grace-Skill-Tree.html's "Full Content Inventory" (2026-08-09), not invented here - that doc
    /// is itself pulled from the real generator code the SHIPPED (old) tree already used, so this
    /// is the same content, re-authored as SkillTreeV2 assets instead of runtime-generated
    /// PrestigeSkillNode entries.
    ///
    /// Does NOT touch the Phase 2/3 dummy/test content (Assets/Config/SkillTreeV2/*.asset,
    /// TestSkillTreeDatabase.asset) - writes to a new Assets/Config/SkillTreeV2/OT/ subfolder and a
    /// new OldTestamentSkillTreeDatabase.asset, then rewires GameLoopController's
    /// skillTreeV2Database field to point at the new real asset. The dummy set stays on disk,
    /// unreferenced, same "deprecate don't delete" pattern used for the old shipped tree.
    ///
    /// Book Mastery content deliberately stays the honest procedural placeholder
    /// (BookMasteryData.curatedNodes left empty) - real per-book differentiated Mastery content is
    /// still an open design question pending the user's own read-through, per this project's
    /// standing "don't invent scripture-adjacent content unilaterally" rule. What's authored here
    /// is real chapter counts, real canonical order, and real thematic titles only where already
    /// established in an earlier design pass - not invented fresh.
    /// </summary>
    public static class SkillTreeV2RealOTContent
    {
        private const string Folder = "Assets/Config/SkillTreeV2/OT";
        private const string IconRoot = "Assets/KyriseRPGIconPack/icons/48x48";

        private class BranchSpec
        {
            public string Key, DisplayName, CapstoneName, IconFile;
            public SkillEffectType EffectType;
            public double[] BaseCosts;
            public double[] EffectPerRank;
        }

        private static readonly int[] Ranks = { 1, 1, 1, 3, 3, 5, 5, 1 };
        private const double EconGrowth = 2.0;

        private static readonly BranchSpec[] Branches =
        {
            new BranchSpec { Key = "InkFlow", DisplayName = "Ink Flow", CapstoneName = "River of Living Ink",
                EffectType = SkillEffectType.IncomeMultiplier, IconFile = "potion_01a.png",
                BaseCosts = new double[]{8,12,18,23,38,60,105,375},
                EffectPerRank = new double[]{.02,.02,.02,.03,.03,.025,.025,.15} },

            new BranchSpec { Key = "SteadyHand", DisplayName = "Steady Hand", CapstoneName = "Scribe's Unshaking Hand",
                EffectType = SkillEffectType.ClickPowerMultiplier, IconFile = "bow_01a.png",
                BaseCosts = new double[]{8,12,18,23,38,60,105,375},
                EffectPerRank = new double[]{.025,.025,.025,.03,.03,.025,.025,.15} },

            new BranchSpec { Key = "OverseersWisdom", DisplayName = "Overseer's Wisdom", CapstoneName = "Joseph's Full Storehouse",
                EffectType = SkillEffectType.ManagerBonusBoost, IconFile = "spellbook_01a.png",
                BaseCosts = new double[]{15,23,33,45,68,98,135,450},
                EffectPerRank = new double[]{.02,.02,.02,.025,.025,.02,.02,.10} },

            new BranchSpec { Key = "IlluminatedPages", DisplayName = "Illuminated Pages", CapstoneName = "Marginalia of the Faithful",
                EffectType = SkillEffectType.ProgressMultiplierBoost, IconFile = "scroll_01a.png",
                BaseCosts = new double[]{9,15,23,30,45,68,98,330},
                EffectPerRank = new double[]{.02,.02,.02,.03,.03,.025,.025,.15} },

            new BranchSpec { Key = "ScribesDiligence", DisplayName = "Scribe's Diligence", CapstoneName = "Tireless Copyist",
                EffectType = SkillEffectType.ScribeMilestoneBoost, IconFile = "candle_01a.png",
                BaseCosts = new double[]{9,15,23,30,45,68,98,330},
                EffectPerRank = new double[]{.02,.02,.02,.025,.025,.02,.02,.10} },

            new BranchSpec { Key = "GraceOfMemorization", DisplayName = "Grace of Memorization", CapstoneName = "Perfect Recall",
                EffectType = SkillEffectType.GraceGainBonus, IconFile = "pearl_01a.png",
                BaseCosts = new double[]{12,18,27,38,53,83,128,450},
                EffectPerRank = new double[]{.02,.02,.02,.03,.03,.025,.025,.15} },

            new BranchSpec { Key = "SwiftUnlock", DisplayName = "Swift Unlock", CapstoneName = "Open Door",
                EffectType = SkillEffectType.PricingDiscount, IconFile = "key_01a.png",
                BaseCosts = new double[]{9,15,23,30,45,68,98,330},
                EffectPerRank = new double[]{.015,.015,.015,.02,.02,.015,.015,.08} },

            new BranchSpec { Key = "ManagersCalling", DisplayName = "Manager's Calling", CapstoneName = "Called Before Their Time",
                EffectType = SkillEffectType.ManagerUnlockLevelDiscount, IconFile = "ring_01a.png",
                BaseCosts = new double[]{15,23,30,45,68,105,150,525},
                EffectPerRank = new double[]{1,1,1,1,1,1,1,3} },
        };

        // Real thematic titles established in the earlier mockup design pass - left blank for
        // every book NOT in this table rather than inventing fresh lore for the remaining ~26.
        private static readonly Dictionary<string, string> ThematicTitles = new Dictionary<string, string>
        {
            { "genesis_1", "Creation's Breath" },
            { "exodus_2", "Covenant Might" },
            { "leviticus_3", "The Sin Offering" },
            { "numbers_4", "The Wilderness Count" },
            { "deuteronomy_5", "The Second Law" },
            { "joshua_6", "Twelve Stones" },
            { "judges_7", "The Cycle of Deliverance" },
            { "ruth_8", "The Kinsman Redeemer" },
            { "1samuel_9", "The Anointed King" },
            { "2samuel_10", "The Everlasting House" },
            { "1kings_11", "The Divided Crown" },
            { "psalms_19", "Songs in the Night" },
            { "micah_33", "What the Lord Requires" },
        };

        // Real chapter counts, pulled directly from Assets/Resources/Bible/kjv_outline.json.
        private static readonly Dictionary<string, int> ChapterCounts = new Dictionary<string, int>
        {
            {"genesis_1",50},{"exodus_2",40},{"leviticus_3",27},{"numbers_4",36},{"deuteronomy_5",34},
            {"joshua_6",24},{"judges_7",21},{"ruth_8",4},{"1samuel_9",31},{"2samuel_10",24},
            {"1kings_11",22},{"2kings_12",25},{"1chronicles_13",29},{"2chronicles_14",36},{"ezra_15",10},
            {"nehemiah_16",13},{"esther_17",10},{"job_18",42},{"psalms_19",150},{"proverbs_20",31},
            {"ecclesiastes_21",12},{"songofsolomon_22",8},{"isaiah_23",66},{"jeremiah_24",52},{"lamentations_25",5},
            {"ezekiel_26",48},{"daniel_27",12},{"hosea_28",14},{"joel_29",3},{"amos_30",9},
            {"obadiah_31",1},{"jonah_32",4},{"micah_33",7},{"nahum_34",3},{"habakkuk_35",3},
            {"zephaniah_36",3},{"haggai_37",2},{"zechariah_38",14},{"malachi_39",4},
        };

        [MenuItem("Tools/Author Real OT Skill Tree V2 Content")]
        public static void Author()
        {
            EnsureFolder("Assets/Config", "SkillTreeV2");
            EnsureFolder("Assets/Config/SkillTreeV2", "OT");

            var core = CreateNode("Core", "Grace Awakened", "Core",
                "The hub. Every branch requires this before anything else can even be seen.",
                8, 1.0, 1, SkillEffectType.IncomeMultiplier, 0,
                SkillNodeShape.Star, new Color(0.95f, 0.82f, 0.45f), LoadIcon("crystal_01a.png"));
            Save(core, "Core");

            var economyNodes = new List<SkillNodeData>();
            var capstones = new List<SkillNodeData>();

            foreach (var branch in Branches)
            {
                var icon = LoadIcon(branch.IconFile);
                var accent = BranchColor(branch.Key);
                SkillNodeData prev = core;
                for (int i = 0; i < 8; i++)
                {
                    bool isCap = i == 7;
                    string displayName = isCap ? branch.CapstoneName : $"{branch.DisplayName} {RomanNumeral(i + 1)}";
                    string desc = DescribeEffect(branch.EffectType, branch.EffectPerRank[i], Ranks[i]) +
                        (isCap ? " Requires a Reset-Prestige to buy." : "");

                    var node = CreateNode($"{branch.Key}_{i + 1}", displayName, branch.DisplayName, desc,
                        branch.BaseCosts[i], EconGrowth, Ranks[i], branch.EffectType, branch.EffectPerRank[i],
                        isCap ? SkillNodeShape.Hexagon : SkillNodeShape.Circle, accent, icon);
                    node.isCapstone = isCap;
                    node.requiresResetPrestige = isCap;
                    node.prerequisites.Add(new SkillNodePrerequisite { node = prev, rankRequired = prev.maxRank });

                    Save(node, $"{branch.Key}_{i + 1}");
                    economyNodes.Add(node);
                    prev = node;
                    if (isCap) capstones.Add(node);
                }
            }

            var convergence = CreateNode("Convergence", "Grace Made Perfect", "Convergence",
                "Requires all 8 branch capstones maxed simultaneously. +25% total Ink/sec, stacks with every other bonus. Opens the Book gateway.",
                5000, 1.0, 1, SkillEffectType.IncomeMultiplier, 0.25,
                SkillNodeShape.Diamond, new Color(0.95f, 0.82f, 0.45f), LoadIcon("staff_01a.png"));
            convergence.requiresResetPrestige = true;
            foreach (var cap in capstones)
                convergence.prerequisites.Add(new SkillNodePrerequisite { node = cap, rankRequired = cap.maxRank });
            Save(convergence, "Convergence");

            var books = new List<BookMasteryData>();
            for (int i = 0; i < CanonicalBookOrder.Books.Length; i++)
            {
                var (resourceId, displayName) = CanonicalBookOrder.Books[i];
                var book = ScriptableObject.CreateInstance<BookMasteryData>();
                book.bookResourceId = resourceId;
                book.displayName = displayName;
                book.thematicTitle = ThematicTitles.TryGetValue(resourceId, out var title) ? title : "";
                book.slotIndex = i + 1; // canonical order, display/authoring reference only - see doc comment
                book.chapterCount = ChapterCounts.TryGetValue(resourceId, out var ch) ? ch : 25;
                // curatedNodes deliberately left empty - real per-book Mastery content is an open
                // design question pending the user's own read-through, not invented here.
                AssetDatabase.CreateAsset(book, $"{Folder}/BookMastery_{resourceId}.asset");
                books.Add(book);
            }

            var database = ScriptableObject.CreateInstance<SkillTreeDatabase>();
            database.core = core;
            database.economyNodes = economyNodes;
            database.convergence = convergence;
            database.books = books;
            AssetDatabase.CreateAsset(database, $"{Folder}/OldTestamentSkillTreeDatabase.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

#if UNITY_EDITOR
            var problems = database.ValidateGraph();
            if (problems.Count > 0)
            {
                Debug.LogWarning($"[SkillTreeV2RealOTContent] ValidateGraph found {problems.Count} issue(s):\n" + string.Join("\n", problems));
            }
            else
            {
                Debug.Log("[SkillTreeV2RealOTContent] ValidateGraph: clean - every node reachable from Core, no null prerequisite slots.");
            }
#endif

            Debug.Log($"[SkillTreeV2RealOTContent] Authored {1 + economyNodes.Count + 1} tree nodes and {books.Count} BookMasteryData assets at {Folder}.");
        }

        [MenuItem("Tools/Rewire GameLoopController To Real OT Skill Tree")]
        public static void RewireGameLoopController()
        {
            var db = AssetDatabase.LoadAssetAtPath<SkillTreeDatabase>($"{Folder}/OldTestamentSkillTreeDatabase.asset");
            if (db == null)
            {
                Debug.LogError("[SkillTreeV2RealOTContent] OldTestamentSkillTreeDatabase.asset not found - run 'Author Real OT Skill Tree V2 Content' first.");
                return;
            }

            var scene = EditorSceneManager_OpenMainMenu();
            var glc = Object.FindAnyObjectByType<ClickerGenesis.Core.GameLoopController>();
            if (glc == null)
            {
                Debug.LogError("[SkillTreeV2RealOTContent] No GameLoopController found in MainMenu.unity.");
                return;
            }

            var so = new SerializedObject(glc);
            so.FindProperty("skillTreeV2Database").objectReferenceValue = db;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(glc);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[SkillTreeV2RealOTContent] GameLoopController.skillTreeV2Database now points at the real OT database.");
        }

        private static UnityEngine.SceneManagement.Scene EditorSceneManager_OpenMainMenu() =>
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

        // ================= helpers =================

        private static SkillNodeData CreateNode(string assetKey, string displayName, string branchCategory, string description,
            double baseCost, double costMultiplier, int maxRank, SkillEffectType effectType, double effectPerRank,
            SkillNodeShape shape, Color accent, Sprite icon)
        {
            var node = ScriptableObject.CreateInstance<SkillNodeData>();
            node.id = assetKey;
            node.displayName = displayName;
            node.branchCategory = branchCategory;
            node.description = description;
            node.baseCost = baseCost;
            node.costMultiplier = costMultiplier;
            node.maxRank = maxRank;
            node.effectType = effectType;
            node.effectPerRank = effectPerRank;
            node.shape = shape;
            node.accentColor = accent;
            node.icon = icon;
            return node;
        }

        private static void Save(SkillNodeData node, string fileName) =>
            AssetDatabase.CreateAsset(node, $"{Folder}/{fileName}.asset");

        private static Sprite LoadIcon(string fileName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconRoot}/{fileName}");
            if (sprite == null)
                Debug.LogWarning($"[SkillTreeV2RealOTContent] Icon not found or not imported as Sprite: {IconRoot}/{fileName}");
            return sprite;
        }

        private static string RomanNumeral(int n) =>
            new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" }[Mathf.Clamp(n - 1, 0, 7)];

        private static string DescribeEffect(SkillEffectType type, double perRank, int maxRank)
        {
            string rankSuffix = maxRank > 1 ? " per rank" : "";
            switch (type)
            {
                case SkillEffectType.IncomeMultiplier: return $"+{perRank * 100:0.#}% total Ink/sec{rankSuffix}.";
                case SkillEffectType.ClickPowerMultiplier: return $"+{perRank * 100:0.#}% tap value{rankSuffix}.";
                case SkillEffectType.ManagerBonusBoost: return $"+{perRank * 100:0.#}% manager bonus{rankSuffix}.";
                case SkillEffectType.ProgressMultiplierBoost: return $"+{perRank * 100:0.#}% progress multiplier{rankSuffix}.";
                case SkillEffectType.ScribeMilestoneBoost: return $"+{perRank * 100:0.#}% scribe milestone curve{rankSuffix}.";
                case SkillEffectType.GraceGainBonus: return $"+{perRank * 100:0.#}% Grace per prestige{rankSuffix}.";
                case SkillEffectType.PricingDiscount: return $"-{perRank * 100:0.#}% verse/chapter Ink cost{rankSuffix}.";
                case SkillEffectType.ManagerUnlockLevelDiscount: return $"-{perRank:0} manager unlock level{rankSuffix}.";
                default: return "";
            }
        }

        private static Color BranchColor(string key)
        {
            switch (key)
            {
                case "InkFlow": return new Color(0.88f, 0.73f, 0.23f);
                case "SteadyHand": return new Color(0.85f, 0.29f, 0.29f);
                case "OverseersWisdom": return new Color(0.29f, 0.56f, 0.85f);
                case "IlluminatedPages": return new Color(0.29f, 0.70f, 0.45f);
                case "ScribesDiligence": return new Color(0.63f, 0.37f, 0.85f);
                case "GraceOfMemorization": return new Color(0.18f, 0.67f, 0.67f);
                case "SwiftUnlock": return new Color(0.88f, 0.53f, 0.18f);
                case "ManagersCalling": return new Color(0.85f, 0.29f, 0.63f);
                default: return Color.white;
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
