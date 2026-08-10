using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ClickerGenesis.Core;

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

        // 2026-08-09, bug #87: HandleNodeHoverExit was a no-op, so the description box kept
        // showing whatever was last hovered even after the mouse left every node - and a purchase
        // made while still hovering the same node (the confirm popup never moves the mouse off it)
        // never re-triggered a fresh OnPointerEnter, so the rank/cost line went stale the instant
        // you bought something. Track the currently-hovered node so both cases can be handled for
        // real: hover-exit clears back to the idle prompt, and a successful purchase re-runs the
        // hover-enter formatting against the node the player is still sitting on.
        private const string IdleDescriptionTitle = "";
        private const string IdleDescriptionBody = "Hover a node";
        private SkillNodeUI hoveredNode;

        [Header("Pan/Zoom (2026-08-09 - ScrollRect can't drive an externally-scaled Content, same")]
        [Header("lesson as the deprecated tree's PannableArea rewrite - hand-rolled here instead)")]
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        private const float MinZoom = 0.4f;
        private const float MaxZoom = 1.5f;
        private const float ZoomStep = 0.15f;

        [Header("Purchase confirmation (2026-08-09 - the deprecated tree had a Yes/No popup")]
        [Header("before spending Grace; ported over here since it was never re-added in V2)")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmMessageLabel;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        private SkillNodeData pendingPurchaseNode;

        private SkillTreeRuntimeState runtime;

        /// <summary>Read from the runtime state, not a separate [SerializeField] (2026-08-09 fix) -
        /// two independently-wired "the database" references was a real bug: an authoring pass
        /// updated GameLoopController's copy but not this scene's own copy, and the tree silently
        /// kept rendering stale content with no error. Single source of truth now.</summary>
        private SkillTreeDatabase database => runtime?.Database;

        private readonly Dictionary<SkillNodeData, SkillNodeUI> nodeUIByData = new Dictionary<SkillNodeData, SkillNodeUI>();
        private readonly List<UIConnectorLine> mainLines = new List<UIConnectorLine>();
        private readonly List<SkillNodeUI> masteryNodeUIs = new List<SkillNodeUI>();
        private readonly List<UIConnectorLine> masteryLines = new List<UIConnectorLine>();

        private void Awake()
        {
            // Real-economy integration (2026-08-09, Phase 3) - the sandbox's own local Grace
            // double is gone. GameLoopController is the persistent singleton that owns the actual
            // save state; it must already be bootstrapped (spawned from MainMenu) before this
            // screen can do anything real, same rule every other screen in the project follows.
            if (!ClickerGenesis.Core.GameLoopController.EnsureBootstrapped()) return;
            runtime = ClickerGenesis.Core.GameLoopController.Instance.SkillsV2;

            BuildMainTree();
            SetupPanZoom();
            if (masteryRoot != null) masteryRoot.SetActive(false);
            if (bookMenuPanel != null) bookMenuPanel.SetActive(false);
            RefreshAll();
        }

        // ================= Pan/Zoom =================
        //
        // ScrollRect's own bounds enforcement fights an externally-driven Content.localScale zoom
        // (same failure mode already documented on the deprecated tree's PannableArea.cs) - rather
        // than fight ScrollRect, disable it here and hand-roll drag-to-pan + scroll-wheel zoom as
        // two independent systems, same fix that was already proven out there.

        private void SetupPanZoom()
        {
            if (scrollRect != null) scrollRect.enabled = false;

            if (content != null && content.parent != null)
            {
                var viewport = content.parent as RectTransform;
                if (viewport != null)
                {
                    var image = viewport.GetComponent<Image>();
                    if (image == null)
                    {
                        image = viewport.gameObject.AddComponent<Image>();
                        image.color = new Color(0f, 0f, 0f, 0f); // fully transparent, still raycastable
                    }
                    image.raycastTarget = true;

                    var pannable = viewport.GetComponent<PannableArea>();
                    if (pannable == null) pannable = viewport.gameObject.AddComponent<PannableArea>();
                    pannable.Content = content;
                }
            }

            if (zoomInButton != null) zoomInButton.onClick.AddListener(() => Zoom(ZoomStep));
            if (zoomOutButton != null) zoomOutButton.onClick.AddListener(() => Zoom(-ZoomStep));

            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(HandleConfirmYes);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(HandleConfirmNo);
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void Update()
        {
            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f) Zoom(Mathf.Sign(scroll) * ZoomStep);
        }

        private void Zoom(float delta)
        {
            if (content == null) return;
            float newScale = Mathf.Clamp(content.localScale.x + delta, MinZoom, MaxZoom);
            content.localScale = new Vector3(newScale, newScale, 1f);
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

            if (!runtime.CanBuy(node)) return; // CanvasGroup already blocks unreachable nodes; silently no-op rather than pop a confirm for an impossible purchase

            if (confirmPanel == null) { BuyNodeNow(node); return; }

            pendingPurchaseNode = node;
            int rank = runtime.GetRank(node);
            double cost = node.GetNextCost(rank);
            if (confirmMessageLabel != null)
                confirmMessageLabel.text = $"Buy {node.displayName} (rank {rank + 1}/{node.maxRank}) for {cost:0} Grace?";
            confirmPanel.SetActive(true);
        }

        private void HandleConfirmYes()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            var node = pendingPurchaseNode;
            pendingPurchaseNode = null;
            if (node != null) BuyNodeNow(node);
        }

        private void HandleConfirmNo()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            pendingPurchaseNode = null;
        }

        private void BuyNodeNow(SkillNodeData node)
        {
            if (!runtime.TryBuy(node)) return;
            RefreshAll();

            // Re-run the hover formatting against whatever node the player is still sitting on -
            // a purchase never moves the mouse, so without this the rank/cost line stays frozen
            // at its pre-purchase values until the player moves off and back on (bug #87).
            if (hoveredNode != null) HandleNodeHoverEnter(hoveredNode);

            if (node == database.convergence && runtime.IsMaxed(node))
                OpenBookMenu();
        }

        public void HandleNodeHoverEnter(SkillNodeUI nodeUI)
        {
            hoveredNode = nodeUI;
            var node = nodeUI.Node;
            if (descriptionTitleLabel != null) descriptionTitleLabel.text = node.displayName;
            if (descriptionBodyLabel == null) return;

            int rank = runtime.GetRank(node);
            bool maxed = runtime.IsMaxed(node);

            // Rank/cost line appended below the flavor description - the popup already asked
            // "buy this?" at click time, but hover is the only place a player can check a node's
            // cost/progress WITHOUT committing to a purchase, so it needs the same numbers.
            string rankLine = node.maxRank > 1 ? $"Rank {rank}/{node.maxRank}" : (rank > 0 ? "Owned" : "Not owned");
            string costLine = maxed ? "MAXED" : $"Next rank: {node.GetNextCost(rank):0} Grace";

            descriptionBodyLabel.text = $"{node.description}\n\n{rankLine}\n{costLine}";
        }

        public void HandleNodeHoverExit(SkillNodeUI nodeUI)
        {
            // Guard against a stale exit event firing after the pointer already moved straight
            // onto a different node (a fresh HandleNodeHoverEnter for the new node) - only clear
            // if the node losing hover is still the one currently tracked as active.
            if (hoveredNode != nodeUI) return;
            hoveredNode = null;
            if (descriptionTitleLabel != null) descriptionTitleLabel.text = IdleDescriptionTitle;
            if (descriptionBodyLabel != null) descriptionBodyLabel.text = IdleDescriptionBody;
        }

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
