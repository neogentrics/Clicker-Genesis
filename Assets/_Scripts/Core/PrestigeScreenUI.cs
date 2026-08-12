using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ClickerGenesis.Progression;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// The Grace skill tree screen, reached from ClickerScreen's Prestige button instead of an
    /// instant prestige action (2026-08-04, user's explicit pivot: "there is supposed to pull up a
    /// secondary screen so that they can spend their grace on something"). Builds all ~100 nodes
    /// procedurally at runtime from GameLoopController.SkillTreeConfig into a radial layout (8
    /// economy branches as spokes around a central hub, plus a longer Book Progression spiral) -
    /// a functional approximation of a Path of Exile-style tree (branches, prerequisites,
    /// multi-rank nodes, connecting lines) rather than a hand-curved pixel clone.
    /// </summary>
    public class PrestigeScreenUI : MonoBehaviour
    {
        [Header("Tree canvas (pannable both axes)")]
        public RectTransform Content;
        public GameObject NodeButtonTemplate;
        public GameObject LineTemplate;
        public RectTransform HubIcon;

        [Header("Header")]
        public TMP_Text GraceLabel;
        public Button PrestigeButton;
        public TMP_Text PrestigeButtonLabel;
        public Button PrestigeResetButton;
        public TMP_Text PrestigeResetButtonLabel;
        public TMP_Text StatusLabel;

        [Header("Zoom (105 nodes need more room to navigate than pan alone gives)")]
        public Button ZoomInButton;
        public Button ZoomOutButton;
        private const float MinZoom = 0.35f;
        private const float MaxZoom = 1.2f;
        private const float ZoomStep = 0.15f;

        [Header("Description box (2026-08-05 - fixed box instead of a floating tooltip, so a\n" +
            "hovered node's description is readable even when it's too small/locked to click)")]
        public TMP_Text DescriptionLabel;
        private const string DescriptionIdleText = "Hover a skill to see what it does.";

        [Header("Purchase confirmation popup (2026-08-05 - buying is no longer instant-on-click)")]
        public GameObject ConfirmPanel;
        public TMP_Text ConfirmMessageLabel;
        public Button ConfirmYesButton;
        public Button ConfirmNoButton;
        private PrestigeSkillNode pendingPurchaseNode;

        /// <summary>Book-choice popup (2026-08-09, user's explicit ask - "when they unlock the next
        /// book, the skill says unlock new book... it gives them the option for a pop up to pick
        /// which book they unlock"). Shown instead of the normal Yes/No confirm popup when a
        /// generic Book Progression node is clicked - lists every OT book not yet unlocked, Grace
        /// is only spent once the player actually picks one (GameLoopController.BuySkillWithBookChoice),
        /// so there's no "bought but no book chosen" state possible.</summary>
        [Header("Book-choice popup (2026-08-09, generic 'Unlock New Book' nodes)")]
        public GameObject BookChoicePanel;
        public TMP_Text BookChoiceTitleLabel;
        public Transform BookChoiceContent;
        public GameObject BookChoiceRowTemplate;
        public Button BookChoiceCancelButton;
        private PrestigeSkillNode pendingBookChoiceNode;
        private readonly List<GameObject> bookChoiceRows = new List<GameObject>();

        private GameLoopController Controller => GameLoopController.Instance;

        private class NodeVisual
        {
            public PrestigeSkillNode Node;
            public Button Button;
            public Image Circle;
            public Image Icon;
            public TMP_Text NameLabel;
            public TMP_Text RankLabel;
            public DescriptionBoxHover Hover;
            /// <summary>One connecting line per prerequisite (2026-08-08 - was a single line, now
            /// a list since a node can require several prerequisites at once, e.g. Convergence's 8
            /// capstone requirements) - empty for nodes with no prerequisite (i.e. Core). Hidden/
            /// shown together with the node itself for progressive visibility (2026-08-06).</summary>
            public List<GameObject> IncomingLines = new List<GameObject>();
            /// <summary>Persistent glow shown once the node is owned (rank &gt; 0), distinct from
            /// the "affordable to buy" brightness step that already exists for unowned nodes
            /// (2026-08-06, user's explicit ask: owned nodes need their own visual state, not just
            /// "brighter than locked"). A same-shape Image sized up and tinted gold BEHIND the node
            /// (2026-08-08 - replaced a UnityEngine.UI.Outline component, which duplicates the
            /// graphic via offset copies; on the new non-circular shapes that read as a visibly
            /// doubled/ghosted second star/diamond/hexagon rather than a glow).</summary>
            public GameObject OwnedGlow;
        }

        /// <summary>One icon per branch (drawn white on top of the branch-colored circle) so nodes
        /// read at a glance instead of being bare colored dots. Pulled from the Homeless/Item_Icons
        /// pack, previously unused - fine to be "bright and colorful" and not match the parchment
        /// theme here, per the user's explicit call that this screen doesn't need to (2026-08-05).
        /// Assigned via BranchIcons (serialized, set once by the scene-build script) rather than
        /// AssetDatabase - AssetDatabase only exists in the Editor and would silently show no icons
        /// at all in an actual build.</summary>
        [System.Serializable]
        public struct BranchIconEntry
        {
            public string Branch;
            public Sprite Icon;
        }

        [Header("Node icons (one per branch)")]
        public List<BranchIconEntry> BranchIcons = new List<BranchIconEntry>();

        private Dictionary<string, Sprite> branchIconLookup;

        private Sprite LookupIcon(string branch)
        {
            if (branchIconLookup == null)
            {
                branchIconLookup = new Dictionary<string, Sprite>();
                foreach (var entry in BranchIcons)
                    if (!string.IsNullOrEmpty(entry.Branch)) branchIconLookup[entry.Branch] = entry.Icon;
            }
            return branchIconLookup.TryGetValue(branch, out var sprite) ? sprite : null;
        }

        private readonly List<NodeVisual> nodeVisuals = new List<NodeVisual>();

        // Distinct bright colors per branch, matching the user's explicit "bright and colorful,
        // doesn't need to match the parchment theme" call for this screen specifically.
        private static readonly Color[] BranchColors =
        {
            new Color(0.91f, 0.75f, 0.20f), // Ink Flow - gold
            new Color(0.75f, 0.20f, 0.20f), // Steady Hand - red
            new Color(0.20f, 0.50f, 0.85f), // Overseer's Wisdom - blue
            new Color(0.20f, 0.70f, 0.35f), // Illuminated Pages - green
            new Color(0.55f, 0.25f, 0.75f), // Scribe's Diligence - purple
            new Color(0.10f, 0.65f, 0.65f), // Grace of Memorization - teal
            new Color(0.90f, 0.45f, 0.10f), // Swift Unlock - orange
            new Color(0.85f, 0.20f, 0.55f), // Manager's Calling - pink
            new Color(0.85f, 0.85f, 0.85f), // Book Progression - white/silver
        };

        // Core hub node - bright warm gold, distinct from every branch color so it reads as "the
        // one thing everything else grows out of" rather than just another branch (2026-08-06).
        private static readonly Color CoreColor = new Color(0.97f, 0.85f, 0.35f);

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (PrestigeButton != null) PrestigeButton.onClick.AddListener(() => HandlePrestige(false));
            if (PrestigeResetButton != null) PrestigeResetButton.onClick.AddListener(() => HandlePrestige(true));
            if (ZoomInButton != null) ZoomInButton.onClick.AddListener(() => Zoom(ZoomStep));
            if (ZoomOutButton != null) ZoomOutButton.onClick.AddListener(() => Zoom(-ZoomStep));
            if (ConfirmYesButton != null) ConfirmYesButton.onClick.AddListener(HandleConfirmYes);
            if (ConfirmNoButton != null) ConfirmNoButton.onClick.AddListener(HandleConfirmNo);
            if (ConfirmPanel != null) ConfirmPanel.SetActive(false);
            if (BookChoiceCancelButton != null) BookChoiceCancelButton.onClick.AddListener(HideBookChoicePopup);
            if (BookChoicePanel != null) BookChoicePanel.SetActive(false);
            DescriptionBoxHover.IdleText = DescriptionIdleText;
            if (DescriptionLabel != null) DescriptionLabel.text = DescriptionIdleText;
            if (Controller != null) Controller.OnStateChanged += Refresh;

            BuildTree();
            Refresh();
        }

        /// <summary>105 nodes spread across a large radial layout need more room to navigate than
        /// panning alone gives - scales Content uniformly, clamped, around its own center.</summary>
        private void Zoom(float delta)
        {
            if (Content == null) return;
            float newScale = Mathf.Clamp(Content.localScale.x + delta, MinZoom, MaxZoom);
            Content.localScale = new Vector3(newScale, newScale, 1f);
        }

        /// <summary>Mouse scroll-wheel zoom (2026-08-05, user's explicit ask) - same clamp/step as
        /// the +/- buttons, just a faster path for a mouse-equipped desktop player. Reads via the
        /// new Input System (2026-08-08 fix) - the legacy UnityEngine.Input class this used to call
        /// throws InvalidOperationException every single frame when Player Settings' Active Input
        /// Handling is set to "Input System Package" only (not "Both"), which this project uses -
        /// silently broke the feature since it shipped. Sign-only (not magnitude-scaled): one wheel
        /// notch always applies exactly one ZoomStep, same granularity as a single +/- button
        /// click, regardless of the raw per-platform scroll-delta units the new API reports.</summary>
        private void Update()
        {
            // While a popup covers the tree (2026-08-09, real user report - "when you scroll
            // through the list of books... it zooms in and out of the background tree"), scroll
            // input must stay with the popup's own ScrollRect, not fall through to the tree's
            // zoom - and Escape should close whichever popup is open, matching every other modal
            // in this project (a second real gap the user found - Cancel/Escape weren't closing
            // the book-choice popup at all).
            bool bookPopupOpen = BookChoicePanel != null && BookChoicePanel.activeSelf;
            bool confirmPopupOpen = ConfirmPanel != null && ConfirmPanel.activeSelf;

            if ((bookPopupOpen || confirmPopupOpen) && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (bookPopupOpen) HideBookChoicePopup();
                if (confirmPopupOpen) HandleConfirmNo();
                return;
            }

            if (bookPopupOpen || confirmPopupOpen) return;

            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f) Zoom(Mathf.Sign(scroll) * ZoomStep);
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        /// <summary>No longer writes a transient "Prestiged! +37 Grace..." message here
        /// (2026-08-06, user's explicit correction: it crammed awkwardly into the same area as the
        /// idle "Hover a skill..." prompt and never went away since nothing ever cleared it).
        /// StatusLabel is now entirely owned by Refresh() below, which drives a persistent
        /// "already prestiged" badge instead - the player doesn't need a one-time toast telling
        /// them what just happened when the Grace counter and the tree unlocking around them
        /// already show it. The button is disabled while ineligible, so PerformPrestige returning
        /// false here isn't reachable through normal play.</summary>
        private void HandlePrestige(bool withReset)
        {
            Controller.PerformPrestige(withReset);
        }

        /// <summary>Deterministic per-node "random" value in [-1, 1], seeded on the node's own id
        /// string (2026-08-09, overall tree shape redesign) - stable across every BuildTree() call
        /// (no persisted state needed) but effectively arbitrary from node to node, which is exactly
        /// what an organic-looking branch wobble needs. Not cryptographic, not meant to be - just
        /// needs to not be a straight line.</summary>
        private static float DeterministicJitter(string nodeId)
        {
            int hash = nodeId.GetHashCode();
            var rng = new System.Random(hash);
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        private void BuildTree()
        {
            var config = Controller.SkillTreeConfig;
            if (config == null || Content == null || NodeButtonTemplate == null) return;

            var byBranch = new Dictionary<string, List<PrestigeSkillNode>>();
            foreach (var node in config.nodes)
            {
                if (!byBranch.TryGetValue(node.branch, out var list))
                    byBranch[node.branch] = list = new List<PrestigeSkillNode>();
                list.Add(node);
            }

            var positions = new Dictionary<string, Vector2>();

            // Core sits on the LEFT edge, everything brackets out to the right (2026-08-09 full
            // shape redesign - real user correction: "why are we still doing this six point star
            // thing... it doesn't have to come out of every side of the star... you could put the
            // star in the far left side and have it branch out into different brackets"). Every
            // branch (all 8 economy branches AND Book Progression, uniformly - no more special
            // casing) gets its own bounded slice of a rightward-facing arc instead of a full-circle
            // spoke. Giving Book Progression a slice of its OWN, sized and clamped exactly like
            // every other branch, is also what fixes the real overlap bug the user found (it used
            // to compute a single fixed angle independently of the economy branches' slots, so nothing
            // structurally prevented it drifting into a neighboring branch's lane) - every branch now
            // provably cannot cross into its neighbor's slice, at any radius, because the drift clamp
            // is a fraction of that branch's own angular width.
            Vector2 coreOffset = new Vector2(-620f, 0f);

            var coreNode = config.nodes.Find(n => n.id == "Core");
            if (coreNode != null)
            {
                positions["Core"] = coreOffset;
                CreateNodeVisual(coreNode, coreOffset, CoreColor);
                var coreGo = Content.Find("Node_Core");
                if (coreGo != null) coreGo.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 150f);
            }
            if (HubIcon != null) HubIcon.gameObject.SetActive(false);

            var allBranches = config.branchOrder;
            const float arcSpan = 150f; // total degrees of the rightward-facing bracket, centered on 0 (east)
            const float baseRadius = 300f;
            const float radiusStep = 195f;
            const float driftPerStep = 3.2f;
            float sliceWidth = allBranches.Count > 0 ? arcSpan / allBranches.Count : arcSpan;
            float maxDrift = sliceWidth * 0.38f;

            var capstonePositions = new List<Vector2>();

            for (int b = 0; b < allBranches.Count; b++)
            {
                string branchName = allBranches[b];
                if (!byBranch.TryGetValue(branchName, out var nodes)) continue;

                float branchAngle = -arcSpan / 2f + sliceWidth * (b + 0.5f);
                Color color = branchName == "Book Progression" ? BranchColors[BranchColors.Length - 1] : BranchColors[b % BranchColors.Length];
                float drift = 0f;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    drift = Mathf.Clamp(drift + DeterministicJitter(node.id) * driftPerStep, -maxDrift, maxDrift);
                    float angle = branchAngle + drift;
                    float radius = baseRadius + i * radiusStep;
                    Vector2 pos = coreOffset + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                    positions[node.id] = pos;
                    CreateNodeVisual(node, pos, color);
                    if (node.isCapstone && branchName != "Book Progression") capstonePositions.Add(pos);
                }
            }

            // Convergence (2026-08-09, "bracket" redesign - real user drawing: several branches
            // merge into a single node, which then re-splits into a further set of nodes gated on
            // having unlocked a book). Positioned at the average of every economy branch's capstone
            // position - a real visual "these lines all meet here" point, not an arbitrary spot.
            // Convergence itself sat completely unplaced before this (a real latent bug - its
            // branch is "Core", which was never one of the arc-loop's branches above).
            Vector2 convergencePos = coreOffset + Vector2.right * (baseRadius + 8 * radiusStep);
            if (capstonePositions.Count > 0)
            {
                Vector2 sum = Vector2.zero;
                foreach (var p in capstonePositions) sum += p;
                convergencePos = sum / capstonePositions.Count;
            }
            var convergenceNode = config.nodes.Find(n => n.id == "Convergence");
            if (convergenceNode != null)
            {
                positions["Convergence"] = convergencePos;
                CreateNodeVisual(convergenceNode, convergencePos, CoreColor);
            }

            // Book Mastery: one node per OT book, all requiring Convergence - fanned out from
            // Convergence's own position in a single ring (not a growing chain, since every Mastery
            // node is an independent rank-1 leaf with the same prerequisite) rather than from Core.
            // This is the literal "re-split after the merge point" half of the bracket.
            if (byBranch.TryGetValue("Book Mastery", out var masteryNodes))
            {
                const float masteryArcSpan = 130f;
                const float masteryRadius = 260f;
                float masterySlice = masteryNodes.Count > 0 ? masteryArcSpan / masteryNodes.Count : masteryArcSpan;
                float masteryMaxDrift = masterySlice * 0.4f;
                Color masteryColor = new Color(0.6f, 0.35f, 0.65f);

                for (int i = 0; i < masteryNodes.Count; i++)
                {
                    var node = masteryNodes[i];
                    float baseAngle = -masteryArcSpan / 2f + masterySlice * (i + 0.5f);
                    float angle = baseAngle + DeterministicJitter(node.id) * masteryMaxDrift;
                    Vector2 pos = convergencePos + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * masteryRadius;
                    positions[node.id] = pos;
                    CreateNodeVisual(node, pos, masteryColor);
                }
            }

            // Connecting lines, drawn after every node has a known position (a node's prerequisite
            // may be defined earlier or later in config.nodes than the node itself, in theory).
            // One line per prerequisite (2026-08-08) - a multi-prerequisite node like Convergence
            // draws a line from every one of its required nodes, visualizing the full dependency
            // set rather than just a single chain step.
            foreach (var node in config.nodes)
            {
                if (node.prerequisites == null || node.prerequisites.Count == 0) continue;
                if (!positions.TryGetValue(node.id, out var to)) continue;
                var nv = nodeVisuals.Find(x => x.Node.id == node.id);
                if (nv == null) continue;

                foreach (var prereq in node.prerequisites)
                {
                    if (!positions.TryGetValue(prereq.nodeId, out var from)) continue;
                    var line = CreateLine(from, to);
                    if (line != null) nv.IncomingLines.Add(line);
                }
            }

            // Progressive visibility (2026-08-06, user's explicit design ask): hide every node
            // whose prerequisite chain isn't satisfied yet, applied once right after the whole
            // tree is built (Refresh() keeps it current afterward as ranks are bought). Nodes
            // aren't destroyed/recreated on reveal - they're built once here and just toggled -
            // simpler and avoids re-running the whole positions/lines pass mid-game.
            ApplyNodeVisibility();
        }

        /// <summary>Hides every node (and its incoming line) whose prerequisite rank isn't met yet
        /// - see PrestigeSkillSystem.PrerequisiteSatisfied for why this deliberately ignores the
        /// separate reset gate. Called once after BuildTree and again every Refresh() so newly
        /// revealed nodes appear the instant their prerequisite is bought.</summary>
        private void ApplyNodeVisibility()
        {
            if (Controller?.Skills == null) return;
            foreach (var v in nodeVisuals)
            {
                bool revealed = Controller.Skills.PrerequisiteSatisfied(v.Node);
                if (v.Button.gameObject.activeSelf != revealed) v.Button.gameObject.SetActive(revealed);
                foreach (var line in v.IncomingLines)
                    if (line != null && line.activeSelf != revealed) line.SetActive(revealed);
            }
        }

        private void CreateNodeVisual(PrestigeSkillNode node, Vector2 position, Color color)
        {
            var go = Instantiate(NodeButtonTemplate, Content);
            go.name = "Node_" + node.id;
            go.SetActive(true);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            float size = node.isCapstone ? 130f : 90f;
            rt.sizeDelta = new Vector2(size, size);

            var circle = go.GetComponent<Image>();
            circle.color = color;
            // Distinct shape per node category (2026-08-08 redesign) - Circle for a normal chain
            // step, Hexagon for branch capstones, Star for Core, Diamond for Book Progression,
            // Triangle for Convergence. Procedurally generated, no new art assets needed.
            circle.sprite = NodeShapeSprites.Get(node.shape);
            circle.type = Image.Type.Simple;

            // Owned-glow (2026-08-06, reworked 2026-08-08) - a same-shape Image sized up and
            // tinted gold, sitting BEHIND the node circle, toggled on once rank > 0. Replaced the
            // old UnityEngine.UI.Outline approach, which duplicates the graphic via offset copies -
            // on the new non-circular node shapes that read as a visibly doubled/ghosted second
            // star/diamond/hexagon rather than a clean glow.
            var glowGo = new GameObject("OwnedGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowGo.transform.SetParent(go.transform, false);
            var glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(size + 22f, size + 22f);
            glowRt.anchoredPosition = Vector2.zero;
            var glowImg = glowGo.GetComponent<Image>();
            glowImg.sprite = NodeShapeSprites.Get(node.shape);
            glowImg.type = Image.Type.Simple;
            glowImg.color = new Color(1f, 0.92f, 0.5f, 0.85f);
            glowGo.transform.SetAsFirstSibling(); // behind the node's own circle
            glowGo.SetActive(false);

            Image iconImgRef = null;
            // Core's shape IS a star now (2026-08-08) - a separate star icon on top of it just
            // doubles up visually ("overlaid with multiple stars", real user report). Skip the
            // icon overlay entirely for Core; every other branch still gets its icon as before.
            var icon = node.id == "Core" ? null : LookupIcon(node.branch);
            if (icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(size * 0.55f, size * 0.55f);
                iconRt.anchoredPosition = new Vector2(0f, size * 0.08f); // nudged up to leave room for RankLabel below
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = icon;
                // White icon reads fine on the darker/saturated branch colors, but Book
                // Progression's near-white circle would wash a white icon out entirely - fall
                // back to a dark tint on light circles based on luminance.
                float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
                iconImg.color = luminance > 0.6f ? new Color(0.15f, 0.1f, 0.05f, 1f) : Color.white;
                iconGo.transform.SetAsFirstSibling(); // behind NameLabel/RankLabel, in front of Circle
                iconImgRef = iconImg;
            }

            // No floating name/status text under nodes anymore (2026-08-08, user's explicit ask -
            // "any text we originally had underneath each of the icons, we don't even have to do
            // that anymore because we have the description box on the top... we can get rid of the
            // floating text"). The DescriptionBox hover already shows the full name/status/cost.
            var nameLabel = go.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
            if (nameLabel != null) nameLabel.gameObject.SetActive(false);

            var rankLabel = go.transform.Find("RankLabel")?.GetComponent<TMP_Text>();
            if (rankLabel != null) rankLabel.gameObject.SetActive(node.maxRank > 1);

            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => ShowConfirmPurchase(node));

            var hover = BuildTooltip(go, node);

            nodeVisuals.Add(new NodeVisual
            {
                Node = node,
                Button = button,
                Circle = circle,
                Icon = iconImgRef,
                NameLabel = nameLabel,
                RankLabel = rankLabel,
                Hover = hover,
                OwnedGlow = glowGo,
            });
        }

        /// <summary>Nodes use the fixed DescriptionBox instead of the floating TooltipOverlay used
        /// everywhere else (2026-08-05, user's idea) - with 105 small nodes packed into the tree,
        /// aiming a hover precisely enough for a floating tooltip was fiddly; a box that's always
        /// in the same place reads the same regardless of node size or hover precision. Text is
        /// set here at creation and then kept current every Refresh() via DescribeNode(node) below,
        /// since the Grace cost changes as ranks are bought.</summary>
        private DescriptionBoxHover BuildTooltip(GameObject nodeGo, PrestigeSkillNode node)
        {
            var hover = nodeGo.AddComponent<DescriptionBoxHover>();
            hover.Target = DescriptionLabel;
            hover.Text = DescribeNode(node);
            return hover;
        }

        /// <summary>A generic "Unlock New Book" Book Progression slot (2026-08-09) - true when the
        /// node's effect is BookUnlock but it has no static book baked in, meaning the player picks
        /// which book it grants at purchase time via the book-choice popup rather than the normal
        /// Yes/No confirm. The 3-node New Testament gate cluster stays a static group-unlock and is
        /// NOT generic, even though it shares the same effect type.</summary>
        private static bool IsGenericBookSlot(PrestigeSkillNode node) =>
            node.effectType == SkillEffectType.BookUnlock && string.IsNullOrEmpty(node.unlockBookResourceId);

        /// <summary>Description text PLUS its live Grace cost (2026-08-05, real bug fix - hovering
        /// a node used to only say what it does, never what it costs). Shows "MAXED" once every
        /// rank is bought instead of a cost that can never be paid again.</summary>
        private string DescribeNode(PrestigeSkillNode node)
        {
            if (Controller?.Skills == null) return $"{node.displayName}\n{node.description}";

            // Node name leads every description now that there's no floating label under the icon
            // (2026-08-08) - the hover box is the only place a node's name/status appears at all.
            string header = node.displayName;

            // A spent generic slot shows what the player actually chose (2026-08-09) instead of
            // "MAXED" alone, which would otherwise say nothing about the real outcome of the
            // purchase - the whole point of the choice was picking a specific book.
            if (IsGenericBookSlot(node) && Controller.Skills.GetRank(node.id) > 0)
            {
                string chosenId = Controller.Skills.GetChosenBook(node.id);
                string chosenName = string.IsNullOrEmpty(chosenId) ? "(unknown)" : ClickerGenesis.Data.CanonicalBookOrder.DisplayNameOf(chosenId);
                return $"{header}\nUnlocked: {chosenName}";
            }

            if (Controller.Skills.IsMaxed(node)) return $"{header}\n{node.description}\nMAXED";

            bool hasReset = Controller.Prestige != null && Controller.Prestige.ResetPrestigeCount > 0;
            if (node.requiresResetPrestige && !hasReset)
                return $"{header}\n{node.description}\n(Requires Reset)";

            // Book Mastery nodes (2026-08-09) - show which book is still needed, same "tell the
            // player what's blocking them" spirit as the Reset gate above.
            if (!string.IsNullOrEmpty(node.requiresBookResourceId) && !Controller.Skills.IsBookUnlocked(node.requiresBookResourceId))
            {
                string neededName = ClickerGenesis.Data.CanonicalBookOrder.DisplayNameOf(node.requiresBookResourceId);
                return $"{header}\n{node.description}\n(Requires {neededName} unlocked)";
            }

            double cost = Controller.Skills.GetNextCost(node);
            return $"{header}\n{node.description}\nCost: {NumberFormatter.FormatWhole(cost)} Grace";
        }

        /// <summary>Buying is no longer instant-on-click (2026-08-05, user's explicit ask) - shows
        /// the cost and waits for Yes/No instead of spending Grace the moment a node is tapped. A
        /// generic Book Progression slot (2026-08-09) routes to the book-choice popup instead of
        /// the normal Yes/No confirm - picking a book there both confirms AND spends Grace in one
        /// step (BuySkillWithBookChoice), so there's no separate confirm step for these.</summary>
        private void ShowConfirmPurchase(PrestigeSkillNode node)
        {
            if (IsGenericBookSlot(node)) { ShowBookChoicePopup(node); return; }
            if (ConfirmPanel == null) { Controller.BuySkill(node.id); return; }

            pendingPurchaseNode = node;
            double cost = Controller.Skills.GetNextCost(node);
            int rank = Controller.Skills.GetRank(node.id);
            if (ConfirmMessageLabel != null)
                ConfirmMessageLabel.text = $"Buy {node.displayName} (rank {rank + 1}/{node.maxRank}) for {NumberFormatter.FormatWhole(cost)} Grace?";
            ConfirmPanel.SetActive(true);
        }

        private void HandleConfirmYes()
        {
            if (pendingPurchaseNode != null) Controller.BuySkill(pendingPurchaseNode.id);
            pendingPurchaseNode = null;
            if (ConfirmPanel != null) ConfirmPanel.SetActive(false);
        }

        private void HandleConfirmNo()
        {
            pendingPurchaseNode = null;
            if (ConfirmPanel != null) ConfirmPanel.SetActive(false);
        }

        /// <summary>Lists every OT book the player hasn't unlocked yet (excluding their own starting
        /// book) as a clickable row - clicking one immediately buys the slot AND assigns that book
        /// in one action (2026-08-09). Rows are rebuilt fresh each time the popup opens rather than
        /// built once and toggled, since which books are still locked changes as the player buys
        /// more slots - this list is small (at most 38 rows) and only rebuilds on a deliberate user
        /// action (opening the popup), not every frame, so it doesn't risk the bug #22 class of
        /// per-frame-rebuild lag.</summary>
        private void ShowBookChoicePopup(PrestigeSkillNode node)
        {
            if (BookChoicePanel == null || BookChoiceContent == null || BookChoiceRowTemplate == null)
            {
                // No popup UI wired (shouldn't happen once the scene is built) - fall back to
                // failing closed rather than silently buying an unassigned slot.
                return;
            }

            pendingBookChoiceNode = node;

            foreach (var row in bookChoiceRows) Destroy(row);
            bookChoiceRows.Clear();

            double cost = Controller.Skills.GetNextCost(node);
            if (BookChoiceTitleLabel != null)
                BookChoiceTitleLabel.text = $"Unlock New Book - choose one ({NumberFormatter.FormatWhole(cost)} Grace):";

            foreach (var (resourceId, displayName) in Controller.AllBooksInOrder)
            {
                if (resourceId == Controller.StartingBookResourceId) continue;
                if (Controller.Skills.IsBookUnlocked(resourceId)) continue;

                var rowGo = Instantiate(BookChoiceRowTemplate, BookChoiceContent);
                rowGo.SetActive(true);
                rowGo.name = "BookChoiceRow_" + resourceId;

                var label = rowGo.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = displayName;

                var button = rowGo.GetComponent<Button>();
                string capturedId = resourceId;
                if (button != null) button.onClick.AddListener(() => HandleBookChosen(capturedId));

                bookChoiceRows.Add(rowGo);
            }

            BookChoicePanel.SetActive(true);
        }

        private void HandleBookChosen(string bookResourceId)
        {
            if (pendingBookChoiceNode != null)
                Controller.BuySkillWithBookChoice(pendingBookChoiceNode.id, bookResourceId);
            HideBookChoicePopup();
        }

        private void HideBookChoicePopup()
        {
            pendingBookChoiceNode = null;
            if (BookChoicePanel != null) BookChoicePanel.SetActive(false);
        }

        private GameObject CreateLine(Vector2 from, Vector2 to)
        {
            if (LineTemplate == null) return null;
            var go = Instantiate(LineTemplate, Content);
            go.name = "Line";
            go.SetActive(true);
            go.transform.SetAsFirstSibling(); // lines render behind nodes

            var rt = go.GetComponent<RectTransform>();
            Vector2 mid = (from + to) / 2f;
            float length = Vector2.Distance(from, to);
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(length, 6f);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            return go;
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Prestige == null) return;

            if (GraceLabel != null)
                GraceLabel.text = $"Grace: {NumberFormatter.FormatWhole(Controller.Prestige.Grace)}";

            // Persistent "already prestiged" badge (2026-08-06 restyle) - purple, bold, always
            // current rather than a one-shot toast set only right after a click. Distinct color
            // from every other text on this screen so it reads as its own status indicator, not
            // another description line.
            if (StatusLabel != null)
            {
                int totalPrestiges = Controller.Prestige.FreePrestigeCount + Controller.Prestige.ResetPrestigeCount;
                StatusLabel.text = totalPrestiges > 0
                    ? $"<b><color=#9B59B6>✦ You have prestiged before ({totalPrestiges}x)</color></b>"
                    : "";
            }

            var levels = Controller.Levels;
            if (PrestigeButton != null)
            {
                bool eligible = levels.IsPrestigeEligible;
                PrestigeButton.interactable = eligible;
                if (PrestigeButtonLabel != null)
                    PrestigeButtonLabel.text = eligible
                        ? $"Prestige (+{NumberFormatter.FormatWhole(Controller.PrestigeGracePreview)} Grace)"
                        : $"Prestige (Level {levels.PrestigeLevelThreshold} required)";
            }
            if (PrestigeResetButton != null)
            {
                bool eligible = levels.IsPrestigeEligible;
                PrestigeResetButton.interactable = eligible;
                if (PrestigeResetButtonLabel != null)
                    PrestigeResetButtonLabel.text = eligible
                        ? $"Reset for +{NumberFormatter.FormatWhole(Controller.PrestigeGracePreviewWithReset)} Grace"
                        : "Reset (Locked)";
            }

            ApplyNodeVisibility();

            double grace = Controller.Prestige.Grace;
            foreach (var v in nodeVisuals)
            {
                if (!v.Button.gameObject.activeSelf) continue; // not revealed yet - nothing to update

                var node = v.Node;
                int rank = Controller.Skills.GetRank(node.id);
                bool hasReset = Controller.Prestige.ResetPrestigeCount > 0;
                bool unlocked = Controller.Skills.IsUnlocked(node, hasReset);
                bool maxed = Controller.Skills.IsMaxed(node);
                bool canBuy = Controller.Skills.CanBuy(node, grace, hasReset);

                v.Button.interactable = canBuy;

                // Brightness tracks OWNERSHIP (rank > 0), not just "unlocked" - 2026-08-05, real
                // user correction: branch-root nodes (no prerequisite) were rendering at near-full
                // brightness before a single rank had been bought, reading as "already claimed"
                // when nothing had. A node the player hasn't spent Grace on yet should look dark
                // regardless of whether its prerequisite is satisfied; only owning at least one
                // rank lights it up. Locked (prerequisite not met, can never be bought yet) is the
                // dimmest; unlocked-but-not-yet-bought is a little brighter so it still reads as
                // reachable; owning any rank (partial or maxed) is fully lit.
                Color c = v.Circle.color;
                float targetAlpha = rank > 0 ? 1f : (unlocked ? 0.5f : 0.3f);
                v.Circle.color = new Color(c.r, c.g, c.b, targetAlpha);
                if (v.Icon != null)
                {
                    Color ic = v.Icon.color;
                    v.Icon.color = new Color(ic.r, ic.g, ic.b, targetAlpha);
                }
                // Owned glow is its own distinct signal from the brightness step above - "lit up"
                // alone already meant "affordable or owned" before this, which didn't distinguish
                // "you could buy this" from "you already did" (2026-08-06 user ask).
                if (v.OwnedGlow != null && v.OwnedGlow.activeSelf != rank > 0) v.OwnedGlow.SetActive(rank > 0);

                if (v.RankLabel != null && node.maxRank > 1)
                    v.RankLabel.text = $"{rank}/{node.maxRank}";

                if (v.Hover != null) v.Hover.Text = DescribeNode(node);
            }
        }
    }
}
