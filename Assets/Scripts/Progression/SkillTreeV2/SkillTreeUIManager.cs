using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// Phase 2 orchestrator - reads a SkillTreeDatabase, lays it out via SkillTreeLayoutGenerator,
    /// spawns SkillNodeUI/UIConnectorLine instances into a ScrollRect, and routes clicks. Doesn't
    /// draw or own any per-node visual logic itself (that's SkillNodeUI/UIConnectorLine) - this
    /// script's job is strictly build-the-scene-graph and route-events, per the user's explicit
    /// "don't build the entire UI in one script" instruction.
    ///
    /// See the accompanying setup notes for the exact Canvas hierarchy this expects - every
    /// [SerializeField] below is a real Inspector-wired reference, not something this script
    /// creates from scratch at runtime.
    /// </summary>
    public class SkillTreeUIManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SkillTreeDatabase database;
        [SerializeField] private double startingGrace = 500;

        [Header("Main Tree (Rule 1: lives inside a ScrollRect)")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform lineLayer;   // sibling under content, BELOW nodeLayer
        [SerializeField] private RectTransform nodeLayer;   // sibling under content, ABOVE lineLayer
        [SerializeField] private SkillNodeUI nodePrefab;
        [SerializeField] private Sprite dashedLineSprite;   // optional - tiled sprite for bridge-style edges

        [Header("Convergence Book Menu (Rule 4)")]
        [SerializeField] private GameObject bookMenuPanel;
        [SerializeField] private RectTransform bookMenuContent;
        [SerializeField] private BookOptionUI bookOptionPrefab;

        [Header("Book Mastery Sub-Tree")]
        [SerializeField] private GameObject masteryRoot;    // separate ScrollRect, hidden until a book is opened
        [SerializeField] private RectTransform masteryContent;
        [SerializeField] private RectTransform masteryLineLayer;
        [SerializeField] private RectTransform masteryNodeLayer;
        [SerializeField] private TMP_Text masteryTitleLabel;

        [Header("Optional description hookup")]
        [SerializeField] private TMP_Text descriptionTitleLabel;
        [SerializeField] private TMP_Text descriptionBodyLabel;
        [SerializeField] private TMP_Text graceReadoutLabel;

        private SkillTreeRuntimeState runtime;
        private readonly Dictionary<SkillNodeData, SkillNodeUI> nodeUIByData = new Dictionary<SkillNodeData, SkillNodeUI>();
        private readonly List<UIConnectorLine> mainLines = new List<UIConnectorLine>();
        private readonly List<SkillNodeUI> masteryNodeUIs = new List<SkillNodeUI>();
        private readonly List<UIConnectorLine> masteryLines = new List<UIConnectorLine>();

        private void Awake()
        {
            runtime = new SkillTreeRuntimeState(startingGrace);
            BuildMainTree();
            if (masteryRoot != null) masteryRoot.SetActive(false);
            if (bookMenuPanel != null) bookMenuPanel.SetActive(false);
            RefreshAll();
        }

        // ================= Main tree construction =================

        private void BuildMainTree()
        {
            if (database == null)
            {
                Debug.LogError("SkillTreeUIManager: no SkillTreeDatabase assigned.");
                return;
            }

            var layout = SkillTreeLayoutGenerator.BuildEconomyLayout(database);

            // Rule 1: Content is sized to the generated bounds so the ScrollRect actually has room
            // to pan across the whole web, not just whatever fits the viewport.
            content.sizeDelta = layout.Bounds.size;
            Vector2 centerOffset = -layout.Bounds.center;

            foreach (var kvp in layout.Positions)
                SpawnNode(kvp.Key, kvp.Value + centerOffset, nodeLayer);

            foreach (var node in database.GetAllNodes())
            {
                if (node?.prerequisites == null) continue;
                if (!layout.Positions.TryGetValue(node, out var toPos)) continue;
                foreach (var prereq in node.prerequisites)
                {
                    if (prereq.node == null) continue;
                    if (!layout.Positions.TryGetValue(prereq.node, out var fromPos)) continue;
                    var line = UIConnectorLine.Create(lineLayer, dashedLineSprite, fromPos + centerOffset, toPos + centerOffset, 3f);
                    line.From = prereq.node;
                    line.To = node;
                    mainLines.Add(line);
                }
            }
        }

        private SkillNodeUI SpawnNode(SkillNodeData data, Vector2 anchoredPos, RectTransform parent)
        {
            var ui = Instantiate(nodePrefab, parent);
            ui.Initialize(data, this);
            ui.Rect.anchoredPosition = anchoredPos;
            if (data == database.core || data == database.convergence)
                ui.Rect.sizeDelta *= 1.6f; // landmark nodes read larger, matching the approved mockup
            nodeUIByData[data] = ui;
            return ui;
        }

        // ================= Refresh =================

        public void RefreshAll()
        {
            foreach (var ui in nodeUIByData.Values) ui.Refresh(runtime);
            foreach (var line in mainLines) line.SetState(ClassifyEdge(line.From, line.To));
            foreach (var ui in masteryNodeUIs) ui.Refresh(runtime);
            foreach (var line in masteryLines) line.SetState(ClassifyEdge(line.From, line.To));

            if (graceReadoutLabel != null) graceReadoutLabel.text = $"Grace: {runtime.Grace:0}";
        }

        private UIConnectorLine.LineState ClassifyEdge(SkillNodeData from, SkillNodeData to)
        {
            if (runtime.GetRank(to) > 0) return UIConnectorLine.LineState.Owned;
            bool toFrontier = to.PrerequisitesSatisfied(runtime.GetRank) && runtime.ExtraGatesSatisfied(to);
            return toFrontier ? UIConnectorLine.LineState.Frontier : UIConnectorLine.LineState.Silhouette;
        }

        // ================= Rule 3 hookups: node click/hover =================

        public void HandleNodeClicked(SkillNodeUI nodeUI)
        {
            var node = nodeUI.Node;

            // Convergence, once already bought, re-opens the book gateway instead of trying to
            // buy another rank of a maxRank-1 node.
            if (node == database.convergence && runtime.IsMaxed(node))
            {
                OpenBookMenu();
                return;
            }

            if (!runtime.TryBuy(node)) return; // CanvasGroup already blocks unreachable nodes; this only silently no-ops on insufficient Grace
            RefreshAll();

            if (node == database.convergence && runtime.IsMaxed(node))
                OpenBookMenu();
        }

        public void HandleNodeHoverEnter(SkillNodeUI nodeUI)
        {
            if (descriptionTitleLabel != null) descriptionTitleLabel.text = nodeUI.Node.displayName;
            if (descriptionBodyLabel != null) descriptionBodyLabel.text = nodeUI.Node.description;
        }

        public void HandleNodeHoverExit(SkillNodeUI nodeUI) { }

        // ================= Rule 4: Convergence Gateway Menu =================

        public void OpenBookMenu()
        {
            if (bookMenuPanel == null || database.books == null) return;

            for (int i = bookMenuContent.childCount - 1; i >= 0; i--)
                Destroy(bookMenuContent.GetChild(i).gameObject);

            foreach (var book in database.books)
            {
                if (book == null) continue;
                var row = Instantiate(bookOptionPrefab, bookMenuContent);
                row.Initialize(book, this);
                row.Refresh(runtime);
            }

            bookMenuPanel.SetActive(true);
        }

        public void CloseBookMenu()
        {
            if (bookMenuPanel != null) bookMenuPanel.SetActive(false);
        }

        public void HandleBookOptionClicked(BookMasteryData book)
        {
            if (!runtime.IsBookUnlocked(book.bookResourceId))
            {
                if (!runtime.TryUnlockBook(book)) return; // not enough Grace - leave the panel open
            }
            CloseBookMenu();
            OpenBookMastery(book);
            RefreshAll();
        }

        // ================= Book Mastery sub-tree =================

        private void OpenBookMastery(BookMasteryData book)
        {
            if (masteryRoot == null) return;

            foreach (var ui in masteryNodeUIs) if (ui != null) Destroy(ui.gameObject);
            masteryNodeUIs.Clear();
            foreach (var line in masteryLines) if (line != null) Destroy(line.gameObject);
            masteryLines.Clear();

            var nodes = (book.curatedNodes != null && book.curatedNodes.Count > 0)
                ? new List<SkillNodeData>(book.curatedNodes)
                : SkillTreeLayoutGenerator.GenerateProceduralMasteryNodes(book);

            var layout = SkillTreeLayoutGenerator.BuildBookMasteryLayout(book, nodes);
            masteryContent.sizeDelta = layout.Bounds.size;
            Vector2 centerOffset = -layout.Bounds.center;

            foreach (var kvp in layout.Positions)
            {
                var ui = Instantiate(nodePrefab, masteryNodeLayer);
                ui.Initialize(kvp.Key, this);
                ui.Rect.anchoredPosition = kvp.Value + centerOffset;
                masteryNodeUIs.Add(ui);
            }

            foreach (var node in nodes)
            {
                if (node?.prerequisites == null) continue;
                if (!layout.Positions.TryGetValue(node, out var toPos)) continue;
                foreach (var prereq in node.prerequisites)
                {
                    if (prereq.node == null) continue;
                    if (!layout.Positions.TryGetValue(prereq.node, out var fromPos)) continue;
                    var line = UIConnectorLine.Create(masteryLineLayer, dashedLineSprite, fromPos + centerOffset, toPos + centerOffset, 3f);
                    line.From = prereq.node;
                    line.To = node;
                    masteryLines.Add(line);
                }
            }

            if (masteryTitleLabel != null)
                masteryTitleLabel.text = string.IsNullOrEmpty(book.thematicTitle)
                    ? $"{book.displayName} Mastery"
                    : $"{book.displayName}: {book.thematicTitle}";

            masteryRoot.SetActive(true);
        }

        public void CloseBookMastery()
        {
            if (masteryRoot != null) masteryRoot.SetActive(false);
        }
    }
}
