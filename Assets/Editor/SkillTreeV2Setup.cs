using System.Collections.Generic;
using ClickerGenesis.Progression;
using ClickerGenesis.Progression.SkillTreeV2;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClickerGenesis.EditorTools
{
    /// <summary>
    /// Temporary automation for standing up a testable Phase 2 Skill Tree V2 scene: generates a
    /// small dummy SkillTreeDatabase (Core -> two branches -> Convergence, plus a 19-node Genesis
    /// and a 3-node Micah Mastery sub-tree), builds the SkillNode/BookOptionRow prefabs, wires a
    /// full Canvas hierarchy around SkillTreeUIManager, and saves it all to a dedicated test scene.
    ///
    /// Deliberately isolated: writes only under Assets/Config/SkillTreeV2, Assets/Prefabs/SkillTreeV2,
    /// and a new Assets/Scenes/SkillTreeV2Test.unity - never touches the shipped
    /// PrestigeScreen.unity or the live PrestigeSkillNode tree. Re-running the menu item wipes and
    /// regenerates all of the above from scratch, so it's safe to run repeatedly while iterating.
    /// </summary>
    public static class SkillTreeV2Setup
    {
        private const string DataFolder = "Assets/Config/SkillTreeV2";
        private const string PrefabFolder = "Assets/Prefabs/SkillTreeV2";
        private const string ScenePath = "Assets/Scenes/SkillTreeV2Test.unity";

        [MenuItem("Tools/Setup Skill Tree V2")]
        public static void RunSetup()
        {
            EnsureFolder("Assets", "Config");
            EnsureFolder("Assets/Config", "SkillTreeV2");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "SkillTreeV2");
            EnsureFolder("Assets", "Scenes");

            var database = BuildDummyDatabase();
            AssetDatabase.SaveAssets();

            var nodePrefab = BuildNodePrefab();
            var bookOptionPrefab = BuildBookOptionPrefab();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene(database, nodePrefab, bookOptionPrefab);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SkillTreeV2Setup] Test scene ready at {ScenePath}. Database: {AssetDatabase.GetAssetPath(database)}");
        }

        // ================= Dummy data =================

        private static SkillTreeDatabase BuildDummyDatabase()
        {
            var core = CreateNode("Core", "Core", "The hub. Every branch requires this before anything else can even be seen.",
                8, 1.0, 1, SkillEffectType.IncomeMultiplier, 0, SkillNodeShape.Star, new Color(0.95f, 0.82f, 0.45f));

            var inkFlowI = CreateNode("Ink Flow I", "Ink Flow", "+2% total Ink/sec per rank.",
                10, 2.0, 2, SkillEffectType.IncomeMultiplier, 0.02, SkillNodeShape.Circle, new Color(0.88f, 0.73f, 0.23f));
            inkFlowI.prerequisites.Add(new SkillNodePrerequisite { node = core, rankRequired = 1 });

            var inkFlowCapstone = CreateNode("Ink Flow — Capstone", "Ink Flow", "+15% total Ink/sec.",
                50, 1.0, 1, SkillEffectType.IncomeMultiplier, 0.15, SkillNodeShape.Hexagon, new Color(0.88f, 0.73f, 0.23f));
            inkFlowCapstone.isCapstone = true;
            inkFlowCapstone.prerequisites.Add(new SkillNodePrerequisite { node = inkFlowI, rankRequired = 2 });

            var steadyHandI = CreateNode("Steady Hand I", "Steady Hand", "+2% tap value per rank.",
                10, 2.0, 2, SkillEffectType.ClickPowerMultiplier, 0.02, SkillNodeShape.Circle, new Color(0.88f, 0.36f, 0.4f));
            steadyHandI.prerequisites.Add(new SkillNodePrerequisite { node = core, rankRequired = 1 });

            var steadyHandCapstone = CreateNode("Steady Hand — Capstone", "Steady Hand", "+15% tap value.",
                50, 1.0, 1, SkillEffectType.ClickPowerMultiplier, 0.15, SkillNodeShape.Hexagon, new Color(0.88f, 0.36f, 0.4f));
            steadyHandCapstone.isCapstone = true;
            steadyHandCapstone.prerequisites.Add(new SkillNodePrerequisite { node = steadyHandI, rankRequired = 2 });

            // Multi-prerequisite AND logic: Convergence requires BOTH capstones simultaneously -
            // exactly the "8 branches" mechanic from the full design, scaled down to 2 for this
            // dummy dataset so the AND gate is still real and testable.
            var convergence = CreateNode("Convergence", "Convergence", "Requires both branch capstones. Opens the Book gateway.",
                100, 1.0, 1, SkillEffectType.IncomeMultiplier, 0.25, SkillNodeShape.Diamond, new Color(0.95f, 0.82f, 0.45f));
            convergence.prerequisites.Add(new SkillNodePrerequisite { node = inkFlowCapstone, rankRequired = 1 });
            convergence.prerequisites.Add(new SkillNodePrerequisite { node = steadyHandCapstone, rankRequired = 1 });

            AssetDatabase.CreateAsset(core, $"{DataFolder}/Core.asset");
            AssetDatabase.CreateAsset(inkFlowI, $"{DataFolder}/InkFlowI.asset");
            AssetDatabase.CreateAsset(inkFlowCapstone, $"{DataFolder}/InkFlowCapstone.asset");
            AssetDatabase.CreateAsset(steadyHandI, $"{DataFolder}/SteadyHandI.asset");
            AssetDatabase.CreateAsset(steadyHandCapstone, $"{DataFolder}/SteadyHandCapstone.asset");
            AssetDatabase.CreateAsset(convergence, $"{DataFolder}/Convergence.asset");

            EnsureFolder(DataFolder, "Genesis");
            var genesisNodes = CreateBookMasteryChainNodes($"{DataFolder}/Genesis", "genesis_1_test", "Genesis", 19, 4);
            var genesis = ScriptableObject.CreateInstance<BookMasteryData>();
            genesis.bookResourceId = "genesis_1_test";
            genesis.displayName = "Genesis";
            genesis.thematicTitle = "Creation's Breath";
            genesis.slotIndex = 1;
            genesis.chapterCount = 50;
            genesis.curatedNodes = genesisNodes;
            AssetDatabase.CreateAsset(genesis, $"{DataFolder}/GenesisMastery.asset");

            EnsureFolder(DataFolder, "Micah");
            var micahNodes = CreateBookMasteryChainNodes($"{DataFolder}/Micah", "micah_test", "Micah", 3, 1);
            var micah = ScriptableObject.CreateInstance<BookMasteryData>();
            micah.bookResourceId = "micah_test";
            micah.displayName = "Micah";
            micah.thematicTitle = "What the Lord Requires";
            micah.slotIndex = 2;
            micah.chapterCount = 7;
            micah.curatedNodes = micahNodes;
            AssetDatabase.CreateAsset(micah, $"{DataFolder}/MicahMastery.asset");

            var database = ScriptableObject.CreateInstance<SkillTreeDatabase>();
            database.core = core;
            database.economyNodes = new List<SkillNodeData> { inkFlowI, inkFlowCapstone, steadyHandI, steadyHandCapstone };
            database.convergence = convergence;
            database.books = new List<BookMasteryData> { genesis, micah };
            AssetDatabase.CreateAsset(database, $"{DataFolder}/TestSkillTreeDatabase.asset");

            return database;
        }

        private static SkillNodeData CreateNode(string displayName, string branch, string description,
            double baseCost, double costMultiplier, int maxRank, SkillEffectType effectType, double effectPerRank,
            SkillNodeShape shape, Color accent)
        {
            var node = ScriptableObject.CreateInstance<SkillNodeData>();
            node.displayName = displayName;
            node.branchCategory = branch;
            node.description = description;
            node.baseCost = baseCost;
            node.costMultiplier = costMultiplier;
            node.maxRank = maxRank;
            node.effectType = effectType;
            node.effectPerRank = effectPerRank;
            node.shape = shape;
            node.accentColor = accent;
            return node;
        }

        /// <summary>Real, saved SkillNodeData assets (not the runtime-only procedural fallback) so
        /// BookMasteryData.curatedNodes actually drives GetEffectiveNodeCount() to the requested
        /// count. Split across spokeCount roots to read as a real sprawling constellation for the
        /// 19-node Genesis case rather than one long single-file chain.</summary>
        private static List<SkillNodeData> CreateBookMasteryChainNodes(string folder, string bookResourceId,
            string bookDisplayName, int totalCount, int spokeCount)
        {
            var nodes = new List<SkillNodeData>();
            int perSpoke = Mathf.CeilToInt((float)totalCount / spokeCount);
            int made = 0;

            for (int s = 0; s < spokeCount && made < totalCount; s++)
            {
                SkillNodeData prev = null;
                for (int k = 0; k < perSpoke && made < totalCount; k++, made++)
                {
                    var node = ScriptableObject.CreateInstance<SkillNodeData>();
                    node.displayName = $"{bookDisplayName} Mastery {made + 1}";
                    node.description = $"Placeholder Mastery node #{made + 1} for {bookDisplayName} (test data).";
                    node.branchCategory = $"{bookDisplayName} Mastery";
                    node.requiresBookResourceId = bookResourceId;
                    node.maxRank = 1;
                    node.baseCost = 30.0 + made * 6.0;
                    node.costMultiplier = 1.0;
                    node.effectType = SkillEffectType.IncomeMultiplier;
                    node.effectPerRank = 0.01;
                    node.shape = SkillNodeShape.Circle;
                    node.accentColor = new Color(0.79f, 0.56f, 0.84f);
                    if (prev != null)
                        node.prerequisites.Add(new SkillNodePrerequisite { node = prev, rankRequired = 1 });

                    AssetDatabase.CreateAsset(node, $"{folder}/{bookDisplayName}Mastery_{made + 1:00}.asset");
                    nodes.Add(node);
                    prev = node;
                }
            }
            return nodes;
        }

        // ================= Prefabs =================

        private static SkillNodeUI BuildNodePrefab()
        {
            var root = new GameObject("SkillNode", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(60, 60);
            root.AddComponent<CanvasGroup>();

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var shapeRT = CreateUI("ShapeImage", rt);
            Stretch(shapeRT);
            var shapeImg = AddImage(shapeRT, new Color(0.85f, 0.75f, 0.42f), knob, true);

            var iconRT = CreateUI("IconImage", rt);
            CenterPivot(iconRT, new Vector2(28, 28));
            var iconImg = AddImage(iconRT, Color.white, null, false);
            iconImg.enabled = false;

            var glowRT = CreateUI("BuyableGlow", rt);
            CenterPivot(glowRT, new Vector2(80, 80));
            var glowImg = AddImage(glowRT, new Color(1f, 0.9f, 0.5f, 0.6f), knob, false);
            glowRT.SetAsFirstSibling(); // render behind the shape/icon, not on top
            glowRT.gameObject.SetActive(false);

            var rankRT = CreateUI("RankText", rt);
            CenterPivot(rankRT, new Vector2(56, 20));
            var rankTxt = AddTMP(rankRT, "", 12, Color.black);

            var costRT = CreateUI("CostText", rt);
            costRT.anchorMin = costRT.anchorMax = new Vector2(0.5f, 0f);
            costRT.pivot = new Vector2(0.5f, 1f);
            costRT.sizeDelta = new Vector2(130, 24);
            costRT.anchoredPosition = new Vector2(0, -6);
            var costTxt = AddTMP(costRT, "", 11, new Color(0.95f, 0.82f, 0.45f));

            var nodeUI = root.AddComponent<SkillNodeUI>();
            var so = new SerializedObject(nodeUI);
            so.FindProperty("shapeImage").objectReferenceValue = shapeImg;
            so.FindProperty("iconImage").objectReferenceValue = iconImg;
            so.FindProperty("rankText").objectReferenceValue = rankTxt;
            so.FindProperty("costText").objectReferenceValue = costTxt;
            so.FindProperty("buyableGlow").objectReferenceValue = glowRT.gameObject;
            so.ApplyModifiedProperties();

            var prefab = (GameObject)PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/SkillNode.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SkillNodeUI>();
        }

        private static BookOptionUI BuildBookOptionPrefab()
        {
            var root = new GameObject("BookOptionRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(320, 50);
            var le = root.AddComponent<LayoutElement>();
            le.preferredWidth = 320;
            le.preferredHeight = 50;
            var bg = AddImage(rt, new Color(0.15f, 0.1f, 0.2f, 0.9f));

            var nameRT = CreateUI("NameText", rt);
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(0.6f, 1);
            nameRT.offsetMin = new Vector2(10, 0);
            nameRT.offsetMax = Vector2.zero;
            var nameTxt = AddTMP(nameRT, "Book", 16, Color.white, TextAlignmentOptions.Left);

            var statusRT = CreateUI("StatusText", rt);
            statusRT.anchorMin = new Vector2(0.6f, 0);
            statusRT.anchorMax = new Vector2(1f, 1);
            statusRT.offsetMin = Vector2.zero;
            statusRT.offsetMax = new Vector2(-10, 0);
            var statusTxt = AddTMP(statusRT, "", 13, new Color(0.95f, 0.82f, 0.45f), TextAlignmentOptions.Right);

            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;

            var optionUI = root.AddComponent<BookOptionUI>();
            var so = new SerializedObject(optionUI);
            so.FindProperty("nameText").objectReferenceValue = nameTxt;
            so.FindProperty("statusText").objectReferenceValue = statusTxt;
            so.FindProperty("button").objectReferenceValue = btn;
            so.ApplyModifiedProperties();

            var prefab = (GameObject)PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/BookOptionRow.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<BookOptionUI>();
        }

        // ================= Scene =================

        /// <summary>Phase 3 integration (2026-08-09) - disables the old shipped tree's UI in
        /// PrestigeScreen.unity (renamed, SetActive(false), NOT deleted, per the user's explicit
        /// "just in case" instruction) and builds the real SkillTreeV2 Canvas hierarchy into that
        /// same scene, reusing BuildScene exactly as-is. Keeps PrestigeScreen.unity's own name and
        /// the existing ClickerScreen "Skill Tree" nav button working unchanged - only what's
        /// INSIDE the scene changes.</summary>
        [MenuItem("Tools/Integrate Skill Tree V2 Into PrestigeScreen")]
        public static void IntegrateIntoPrestigeScreen()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrestigeScreen.unity", OpenSceneMode.Single);

            if (GameObject.Find("SkillTreeScreen") != null)
            {
                Debug.LogWarning("[SkillTreeV2Setup] SkillTreeScreen already exists in PrestigeScreen.unity - skipping to avoid duplicating the Canvas. Delete it manually first if you want a clean rebuild.");
                return;
            }

            var oldUI = Object.FindFirstObjectByType<ClickerGenesis.Core.PrestigeScreenUI>(FindObjectsInactive.Include);
            if (oldUI != null)
            {
                var oldRoot = oldUI.transform.root.gameObject;
                oldRoot.name = "OldPrestigeTree_DEPRECATED";
                oldRoot.SetActive(false);
                Debug.Log($"[SkillTreeV2Setup] Disabled old tree root: {oldRoot.name}");
            }
            else
            {
                Debug.LogWarning("[SkillTreeV2Setup] No PrestigeScreenUI found in PrestigeScreen.unity - nothing to disable (already migrated?).");
            }

            var database = AssetDatabase.LoadAssetAtPath<SkillTreeDatabase>($"{DataFolder}/TestSkillTreeDatabase.asset");
            var nodePrefabGO = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/SkillNode.prefab");
            var nodePrefab = nodePrefabGO.GetComponent<SkillNodeUI>();
            var bookOptionPrefabGO = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/BookOptionRow.prefab");
            var bookOptionPrefab = bookOptionPrefabGO.GetComponent<BookOptionUI>();

            BuildScene(database, nodePrefab, bookOptionPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SkillTreeV2Setup] SkillTreeV2 integrated into PrestigeScreen.unity.");
        }

        private static void BuildScene(SkillTreeDatabase database, SkillNodeUI nodePrefab, BookOptionUI bookOptionPrefab)
        {
            var canvasGO = new GameObject("SkillTreeScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var canvasRT = (RectTransform)canvasGO.transform;

            EnsureEventSystem();

            // ---- Main tree ScrollRect ----
            var (mainScroll, mainContent) = CreateScrollStructure("MainScrollRect", canvasRT);
            var lineLayer = CreateUI("LineLayer", mainContent);
            CenterPivot(lineLayer, Vector2.zero);
            var nodeLayer = CreateUI("NodeLayer", mainContent);
            CenterPivot(nodeLayer, Vector2.zero);

            // ---- Mastery sub-tree (hidden until a book is chosen) ----
            var masteryRoot = CreateUI("MasteryRoot", canvasRT);
            Stretch(masteryRoot);
            var masteryTitleRT = CreateUI("MasteryTitleLabel", masteryRoot);
            masteryTitleRT.anchorMin = masteryTitleRT.anchorMax = new Vector2(0.5f, 1f);
            masteryTitleRT.pivot = new Vector2(0.5f, 1f);
            masteryTitleRT.sizeDelta = new Vector2(700, 50);
            masteryTitleRT.anchoredPosition = new Vector2(0, -16);
            var masteryTitle = AddTMP(masteryTitleRT, "", 26, Color.white);
            var (masteryScroll, masteryContent) = CreateScrollStructure("MasteryScrollRect", masteryRoot);
            var masteryLineLayer = CreateUI("MasteryLineLayer", masteryContent);
            CenterPivot(masteryLineLayer, Vector2.zero);
            var masteryNodeLayer = CreateUI("MasteryNodeLayer", masteryContent);
            CenterPivot(masteryNodeLayer, Vector2.zero);
            CreateBackButton("BackButton", masteryRoot, "< Back", null); // wired below once manager exists

            // ---- Convergence book menu (hidden until Convergence is bought and clicked) ----
            var bookMenuPanel = CreateUI("BookMenuPanel", canvasRT);
            CenterPivot(bookMenuPanel, new Vector2(420, 420));
            AddImage(bookMenuPanel, new Color(0.05f, 0.03f, 0.08f, 0.95f));
            var (bookScroll, bookMenuContentInner) = CreateScrollStructure("BookMenuScrollRect", bookMenuPanel);
            var vlg = bookMenuContentInner.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            var fitter = bookMenuContentInner.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateBackButton("CloseButton", bookMenuPanel, "X", null);

            // ---- Description panel ----
            var descPanel = CreateUI("DescriptionPanel", canvasRT);
            descPanel.anchorMin = descPanel.anchorMax = new Vector2(0, 0);
            descPanel.pivot = new Vector2(0, 0);
            descPanel.sizeDelta = new Vector2(340, 120);
            descPanel.anchoredPosition = new Vector2(20, 20);
            AddImage(descPanel, new Color(0.05f, 0.05f, 0.08f, 0.85f));
            var descTitleRT = CreateUI("Title", descPanel);
            descTitleRT.anchorMin = new Vector2(0, 1);
            descTitleRT.anchorMax = new Vector2(1, 1);
            descTitleRT.pivot = new Vector2(0.5f, 1);
            descTitleRT.sizeDelta = new Vector2(-20, 30);
            descTitleRT.anchoredPosition = new Vector2(0, -8);
            var descTitle = AddTMP(descTitleRT, "Hover a node", 16, new Color(0.95f, 0.82f, 0.45f), TextAlignmentOptions.TopLeft);
            var descBodyRT = CreateUI("Body", descPanel);
            descBodyRT.anchorMin = Vector2.zero;
            descBodyRT.anchorMax = Vector2.one;
            descBodyRT.offsetMin = new Vector2(10, 10);
            descBodyRT.offsetMax = new Vector2(-10, -40);
            var descBody = AddTMP(descBodyRT, "", 12, new Color(0.8f, 0.8f, 0.85f), TextAlignmentOptions.TopLeft);

            // ---- Grace readout ----
            var graceRT = CreateUI("GraceReadoutLabel", canvasRT);
            graceRT.anchorMin = graceRT.anchorMax = new Vector2(0, 1);
            graceRT.pivot = new Vector2(0, 1);
            graceRT.sizeDelta = new Vector2(260, 40);
            graceRT.anchoredPosition = new Vector2(20, -20);
            var graceLabel = AddTMP(graceRT, "Grace: 0", 20, new Color(0.95f, 0.82f, 0.45f), TextAlignmentOptions.Left);

            masteryRoot.gameObject.SetActive(false);
            bookMenuPanel.gameObject.SetActive(false);

            // ---- Manager ----
            var managerGO = new GameObject("SkillTreeUIManager", typeof(RectTransform));
            managerGO.transform.SetParent(canvasRT, false);
            var manager = managerGO.AddComponent<SkillTreeUIManager>();

            var mso = new SerializedObject(manager);
            mso.FindProperty("scrollRect").objectReferenceValue = mainScroll;
            mso.FindProperty("content").objectReferenceValue = mainContent;
            mso.FindProperty("lineLayer").objectReferenceValue = lineLayer;
            mso.FindProperty("nodeLayer").objectReferenceValue = nodeLayer;
            mso.FindProperty("nodePrefab").objectReferenceValue = nodePrefab;
            mso.FindProperty("bookMenuPanel").objectReferenceValue = bookMenuPanel.gameObject;
            mso.FindProperty("bookMenuContent").objectReferenceValue = bookMenuContentInner;
            mso.FindProperty("bookOptionPrefab").objectReferenceValue = bookOptionPrefab;
            mso.FindProperty("masteryRoot").objectReferenceValue = masteryRoot.gameObject;
            mso.FindProperty("masteryContent").objectReferenceValue = masteryContent;
            mso.FindProperty("masteryLineLayer").objectReferenceValue = masteryLineLayer;
            mso.FindProperty("masteryNodeLayer").objectReferenceValue = masteryNodeLayer;
            mso.FindProperty("masteryTitleLabel").objectReferenceValue = masteryTitle;
            mso.FindProperty("descriptionTitleLabel").objectReferenceValue = descTitle;
            mso.FindProperty("descriptionBodyLabel").objectReferenceValue = descBody;
            mso.FindProperty("graceReadoutLabel").objectReferenceValue = graceLabel;
            mso.ApplyModifiedProperties();

            // Wire the two Back buttons now that the manager (and its public Close methods) exist.
            var masteryBack = masteryRoot.Find("BackButton").GetComponent<Button>();
            UnityEventTools.AddPersistentListener(masteryBack.onClick, manager.CloseBookMastery);
            var bookMenuClose = bookMenuPanel.Find("CloseButton").GetComponent<Button>();
            UnityEventTools.AddPersistentListener(bookMenuClose.onClick, manager.CloseBookMenu);
        }

        private static Button CreateBackButton(string name, RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var rt = CreateUI(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(110, 40);
            rt.anchoredPosition = new Vector2(20, -20);
            var bg = AddImage(rt, new Color(0.2f, 0.15f, 0.05f, 0.9f));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var textRT = CreateUI("Label", rt);
            Stretch(textRT);
            AddTMP(textRT, label, 16, Color.white);
            if (action != null) UnityEventTools.AddPersistentListener(btn.onClick, action);
            return btn;
        }

        // ================= Small UI helpers =================

        private static RectTransform CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void CenterPivot(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Image AddImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = true)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        private static TMP_Text AddTMP(RectTransform rt, string text, float fontSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>ScrollRect + Viewport + Content, per Rule 1 - RectMask2D on the viewport
        /// (not Mask, which needs a rendering Graphic to write its stencil and would hide
        /// everything behind a fully transparent Image; RectMask2D clips on RectTransform bounds
        /// alone, a real gotcha hit and documented elsewhere in this project already).</summary>
        private static (ScrollRect scroll, RectTransform content) CreateScrollStructure(string name, RectTransform parent)
        {
            var root = CreateUI(name, parent);
            Stretch(root);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateUI("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateUI("Content", viewport);
            CenterPivot(content, new Vector2(2000, 2000)); // real size assigned at runtime by SkillTreeUIManager

            scroll.viewport = viewport;
            scroll.content = content;
            return (scroll, content);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null) go.AddComponent(inputModuleType);
            else go.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
