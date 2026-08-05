using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

        private GameLoopController Controller => GameLoopController.Instance;

        private class NodeVisual
        {
            public PrestigeSkillNode Node;
            public Button Button;
            public Image Circle;
            public TMP_Text NameLabel;
            public TMP_Text RankLabel;
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

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (PrestigeButton != null) PrestigeButton.onClick.AddListener(() => HandlePrestige(false));
            if (PrestigeResetButton != null) PrestigeResetButton.onClick.AddListener(() => HandlePrestige(true));
            if (Controller != null) Controller.OnStateChanged += Refresh;

            BuildTree();
            Refresh();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void HandlePrestige(bool withReset)
        {
            double grace = withReset ? Controller.PrestigeGracePreviewWithReset : Controller.PrestigeGracePreview;
            bool ok = Controller.PerformPrestige(withReset);
            if (StatusLabel != null)
                StatusLabel.text = ok
                    ? (withReset
                        ? $"Prestiged with reset! +{NumberFormatter.FormatWhole(grace)} Grace. Ink and upgrade levels reset - every unlocked verse still stays."
                        : $"Prestiged! +{NumberFormatter.FormatWhole(grace)} Grace. Ink, upgrades, and every unlocked verse stay.")
                    : "Not eligible to prestige yet.";
        }

        private void BuildTree()
        {
            var config = Controller.SkillTreeConfig;
            if (config == null || Content == null || NodeButtonTemplate == null) return;

            var branches = config.branchOrder;
            var byBranch = new Dictionary<string, List<PrestigeSkillNode>>();
            foreach (var node in config.nodes)
            {
                if (!byBranch.TryGetValue(node.branch, out var list))
                    byBranch[node.branch] = list = new List<PrestigeSkillNode>();
                list.Add(node);
            }

            var positions = new Dictionary<string, Vector2>();

            for (int b = 0; b < branches.Count; b++)
            {
                string branchName = branches[b];
                if (!byBranch.TryGetValue(branchName, out var nodes)) continue;

                float branchAngle = (360f / branches.Count) * b;
                bool isBookBranch = branchName == "Book Progression";
                float radiusStep = isBookBranch ? 70f : 150f;
                Color color = BranchColors[b % BranchColors.Length];

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    // Book Progression drifts its angle slightly per node so its long chain reads
                    // as a spiral arm instead of one straight overlapping line of 41 circles.
                    float angle = branchAngle + (isBookBranch ? i * 3f : 0f);
                    float radius = 220f + i * radiusStep;
                    Vector2 pos = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                    positions[node.id] = pos;

                    CreateNodeVisual(node, pos, color);
                }
            }

            // Connecting lines, drawn after every node has a known position (a node's prerequisite
            // may be defined earlier or later in config.nodes than the node itself, in theory).
            foreach (var node in config.nodes)
            {
                if (string.IsNullOrEmpty(node.prerequisiteId)) continue;
                if (!positions.TryGetValue(node.id, out var to)) continue;
                if (!positions.TryGetValue(node.prerequisiteId, out var from)) continue;
                CreateLine(from, to);
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

            var nameLabel = go.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
            if (nameLabel != null) nameLabel.text = node.displayName;

            var rankLabel = go.transform.Find("RankLabel")?.GetComponent<TMP_Text>();
            if (rankLabel != null) rankLabel.gameObject.SetActive(node.maxRank > 1);

            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => Controller.BuySkill(node.id));

            BuildTooltip(go, node);

            nodeVisuals.Add(new NodeVisual
            {
                Node = node,
                Button = button,
                Circle = circle,
                NameLabel = nameLabel,
                RankLabel = rankLabel,
            });
        }

        /// <summary>Same dark-panel-plus-centered-text look as the existing PauseButton/Prestige
        /// tooltips (HoverTooltip.cs), built per node here rather than requiring the template to
        /// carry one - the description text is static per node so it only needs setting once.</summary>
        private void BuildTooltip(GameObject nodeGo, PrestigeSkillNode node)
        {
            var tooltipGo = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tooltipGo.transform.SetParent(nodeGo.transform, false);
            var ttRt = tooltipGo.GetComponent<RectTransform>();
            ttRt.anchorMin = new Vector2(0.5f, 1f);
            ttRt.anchorMax = new Vector2(0.5f, 1f);
            ttRt.pivot = new Vector2(0.5f, 0f);
            ttRt.anchoredPosition = new Vector2(0f, 10f);
            ttRt.sizeDelta = new Vector2(320f, 60f);
            tooltipGo.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.05f, 0.92f);
            tooltipGo.SetActive(false);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(tooltipGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 4);
            textRt.offsetMax = new Vector2(-10, -4);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = node.description;
            tmp.fontSize = 20;
            tmp.color = new Color(0.957f, 0.925f, 0.847f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var hover = nodeGo.AddComponent<HoverTooltip>();
            hover.TooltipObject = tooltipGo;
        }

        private void CreateLine(Vector2 from, Vector2 to)
        {
            if (LineTemplate == null) return;
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
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Prestige == null) return;

            if (GraceLabel != null)
                GraceLabel.text = $"Grace: {NumberFormatter.FormatWhole(Controller.Prestige.Grace)}";

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

            double grace = Controller.Prestige.Grace;
            foreach (var v in nodeVisuals)
            {
                var node = v.Node;
                int rank = Controller.Skills.GetRank(node.id);
                bool unlocked = Controller.Skills.IsUnlocked(node);
                bool maxed = Controller.Skills.IsMaxed(node);
                bool canBuy = Controller.Skills.CanBuy(node, grace);

                v.Button.interactable = canBuy;

                Color c = v.Circle.color;
                float targetAlpha = unlocked ? 1f : 0.35f;
                v.Circle.color = new Color(c.r, c.g, c.b, targetAlpha);

                if (v.RankLabel != null && node.maxRank > 1)
                    v.RankLabel.text = $"{rank}/{node.maxRank}";

                if (v.NameLabel != null)
                {
                    string suffix = maxed ? " (Max)" : !unlocked ? " (Locked)" : "";
                    v.NameLabel.text = node.displayName + suffix;
                }
            }
        }
    }
}
