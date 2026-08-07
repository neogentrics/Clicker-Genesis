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
            /// <summary>The connecting line drawn from this node's prerequisite to this node (null
            /// for nodes with no prerequisite, i.e. Core) - hidden/shown together with the node
            /// itself for progressive visibility (2026-08-06).</summary>
            public GameObject IncomingLine;
            /// <summary>Persistent glow ring shown once the node is owned (rank &gt; 0), distinct
            /// from the "affordable to buy" brightness step that already exists for unowned nodes
            /// (2026-08-06, user's explicit ask: owned nodes need their own visual state, not just
            /// "brighter than locked").</summary>
            public Outline OwnedGlow;
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

            // Central Core node, dead center (2026-08-06 hub redesign) - every branch root now
            // requires this at rank 1, replacing the old static "Grace" text box that used to just
            // sit there doing nothing. Sized up a bit past a normal node so it reads as the true
            // hub, not just another skill.
            var coreNode = config.nodes.Find(n => n.id == "Core");
            if (coreNode != null)
            {
                positions["Core"] = Vector2.zero;
                CreateNodeVisual(coreNode, Vector2.zero, CoreColor);
                var coreGo = Content.Find("Node_Core");
                if (coreGo != null) coreGo.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 150f);
            }
            if (HubIcon != null) HubIcon.gameObject.SetActive(false);

            // Economy branches: 8 straight spokes from the hub, evenly spaced. Radius widened
            // (2026-08-05, user's explicit "spread this out, can't read the labels" correction)
            // so the arc length between neighboring spokes clears the ~220-wide name label at
            // every ring, not just the outer ones.
            var economyBranches = config.branchOrder.FindAll(b => b != "Book Progression");
            const float economyBaseRadius = 320f;
            const float economyRadiusStep = 210f;
            float economyMaxRadius = economyBaseRadius + 7 * economyRadiusStep;

            for (int b = 0; b < economyBranches.Count; b++)
            {
                string branchName = economyBranches[b];
                if (!byBranch.TryGetValue(branchName, out var nodes)) continue;

                float branchAngle = (360f / economyBranches.Count) * b;
                Color color = BranchColors[b % BranchColors.Length];

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    float radius = economyBaseRadius + i * economyRadiusStep;
                    Vector2 pos = new Vector2(Mathf.Cos(branchAngle * Mathf.Deg2Rad), Mathf.Sin(branchAngle * Mathf.Deg2Rad)) * radius;
                    positions[node.id] = pos;
                    CreateNodeVisual(node, pos, color);
                }
            }

            // Book Progression: a full outer ring encircling the whole economy wheel instead of
            // the old "angle += i*3f" spiral-drift hack, which read as an ugly curving branch and
            // crammed 41 nodes' labels into overlapping space. One node per remaining OT book +
            // 3 NT gate nodes, spread evenly across the full 360 degrees in canonical order and
            // connected node-to-node (existing prerequisite-line code below draws the chain
            // automatically) - the ring sits entirely outside the economy spokes' reach, framing
            // them the way a wheel's rim frames its spokes. Gap tightened 380->120 (2026-08-06,
            // real user report: "no one's gonna scroll around looking for those books" - the ring
            // sat too far from everything else at the wide gap. Deliberately kept the ring shape
            // rather than reverting to a literal spiral - that's the exact layout bug #47 replaced
            // this with in the first place ("curves up to the right and then to the left... can't
            // even read the descriptions"), and a spiral tight enough to sit close to center would
            // reproduce that same overlap problem. A tighter ring solves the same "too far away"
            // complaint without bringing back the one it fixed - flagged for the user to weigh in
            // on if they specifically want the interleaved-spiral behavior instead.
            if (byBranch.TryGetValue("Book Progression", out var bookNodes))
            {
                float bookRingRadius = economyMaxRadius + 120f;
                Color bookColor = BranchColors[BranchColors.Length - 1];

                // Half-spoke phase offset (2026-08-06 fix) - without this, the ring's first node
                // (Exodus) landed at angle 0, exactly along the Ink Flow spoke's own radial line,
                // so the Core->Exodus connector visually overlapped Ink Flow's entire spoke and
                // read as "Ink Flow unlocks Exodus" (real user report). Offsetting by half the
                // spoke spacing (22.5 degrees for 8 economy branches) keeps every book node's
                // radial line clear of every economy spoke's line.
                float phaseOffset = economyBranches.Count > 0 ? (360f / economyBranches.Count) / 2f : 0f;

                for (int i = 0; i < bookNodes.Count; i++)
                {
                    var node = bookNodes[i];
                    float angle = (360f / bookNodes.Count) * i + phaseOffset;
                    Vector2 pos = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * bookRingRadius;
                    positions[node.id] = pos;
                    CreateNodeVisual(node, pos, bookColor);
                }
            }

            // Connecting lines, drawn after every node has a known position (a node's prerequisite
            // may be defined earlier or later in config.nodes than the node itself, in theory).
            foreach (var node in config.nodes)
            {
                if (string.IsNullOrEmpty(node.prerequisiteId)) continue;
                if (!positions.TryGetValue(node.id, out var to)) continue;
                if (!positions.TryGetValue(node.prerequisiteId, out var from)) continue;
                var line = CreateLine(from, to);
                var nv = nodeVisuals.Find(x => x.Node.id == node.id);
                if (nv != null) nv.IncomingLine = line;
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
                if (v.IncomingLine != null && v.IncomingLine.activeSelf != revealed) v.IncomingLine.SetActive(revealed);
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

            // Owned-glow ring (2026-08-06) - a real Outline component instead of a new sprite
            // asset, so "this is active" is visible at a glance regardless of how the background
            // renders. Starts disabled; Refresh() turns it on once rank > 0.
            var glow = go.AddComponent<Outline>();
            glow.effectColor = new Color(1f, 0.95f, 0.55f, 0.95f);
            glow.effectDistance = new Vector2(5f, 5f);
            glow.useGraphicAlpha = false;
            glow.enabled = false;

            Image iconImgRef = null;
            var icon = LookupIcon(node.branch);
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

            var nameLabel = go.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
            if (nameLabel != null) nameLabel.text = node.displayName;

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
                OwnedGlow = glow,
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

        /// <summary>Description text PLUS its live Grace cost (2026-08-05, real bug fix - hovering
        /// a node used to only say what it does, never what it costs). Shows "MAXED" once every
        /// rank is bought instead of a cost that can never be paid again.</summary>
        private string DescribeNode(PrestigeSkillNode node)
        {
            if (Controller?.Skills == null) return node.description;
            if (Controller.Skills.IsMaxed(node)) return $"{node.description}\nMAXED";
            double cost = Controller.Skills.GetNextCost(node);
            return $"{node.description}\nCost: {NumberFormatter.FormatWhole(cost)} Grace";
        }

        /// <summary>Buying is no longer instant-on-click (2026-08-05, user's explicit ask) - shows
        /// the cost and waits for Yes/No instead of spending Grace the moment a node is tapped.</summary>
        private void ShowConfirmPurchase(PrestigeSkillNode node)
        {
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
                if (v.OwnedGlow != null) v.OwnedGlow.enabled = rank > 0;

                if (v.RankLabel != null && node.maxRank > 1)
                    v.RankLabel.text = $"{rank}/{node.maxRank}";

                if (v.NameLabel != null)
                {
                    // Reset-gated nodes get their own label (2026-08-06) instead of a generic
                    // "(Locked)" - a player who's already maxed the prerequisite chain needs to
                    // know THIS is why they still can't buy it, not just that something's missing.
                    string suffix = maxed ? " (Max)"
                        : (node.requiresResetPrestige && !hasReset) ? " (Requires Reset)"
                        : !unlocked ? " (Locked)" : "";
                    v.NameLabel.text = node.displayName + suffix;
                }

                if (v.Hover != null) v.Hover.Text = DescribeNode(node);
            }
        }
    }
}
