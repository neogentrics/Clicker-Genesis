using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// Sits on the node prefab. Reads its assigned SkillNodeData plus the manager's
    /// SkillTreeRuntimeState and renders exactly one of three states every Refresh() - no
    /// intermediate/blended visuals, matching the approved mockup's explicit three-state rule:
    ///
    ///   Owned      - full opacity, node's real accent color, rank/cost text visible.
    ///   Frontier   - same full-detail rendering as Owned, but not yet bought; cost text shown,
    ///                a pulsing glow child object toggled on when currently affordable.
    ///   Silhouette - flat grey shape, no text at all, not interactable, no raycast target - a
    ///                blocked node reads as pure geometry, nothing more.
    ///
    /// Uses IPointerClickHandler/IPointerEnterHandler/IPointerExitHandler directly rather than a
    /// Button component - a CanvasGroup toggling blocksRaycasts is enough to make a Silhouette node
    /// fully unclickable, so no Selectable/Button machinery is needed on top.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SkillNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum VisualState { Owned, Frontier, Silhouette }

        [Header("Refs")]
        [SerializeField] private Image shapeImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private GameObject buyableGlow;

        private static readonly Color SilhouetteColor = new Color(0.27f, 0.29f, 0.36f, 1f);

        public SkillNodeData Node { get; private set; }
        public RectTransform Rect { get; private set; }
        public VisualState CurrentState { get; private set; }

        private SkillTreeUIManager manager;
        private CanvasGroup canvasGroup;
        private Image buyableGlowImage;
        private float pulseTimer;

        private void Awake()
        {
            Rect = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            if (buyableGlow != null) buyableGlowImage = buyableGlow.GetComponent<Image>();
        }

        public void Initialize(SkillNodeData node, SkillTreeUIManager owner)
        {
            Node = node;
            manager = owner;
        }

        public void Refresh(SkillTreeRuntimeState runtime)
        {
            if (Node == null || runtime == null) return;

            int rank = runtime.GetRank(Node);
            bool maxed = runtime.IsMaxed(Node);
            bool prereqOk = Node.PrerequisitesSatisfied(runtime.GetRank) && runtime.ExtraGatesSatisfied(Node);

            CurrentState = rank > 0 ? VisualState.Owned : (prereqOk ? VisualState.Frontier : VisualState.Silhouette);

            switch (CurrentState)
            {
                case VisualState.Owned:
                    ApplyDetailVisual(rank, maxed, runtime);
                    break;
                case VisualState.Frontier:
                    ApplyDetailVisual(rank, maxed, runtime);
                    break;
                case VisualState.Silhouette:
                    ApplySilhouetteVisual();
                    break;
            }
        }

        private void ApplyDetailVisual(int rank, bool maxed, SkillTreeRuntimeState runtime)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (shapeImage != null) shapeImage.color = Node.accentColor;
            if (iconImage != null)
            {
                iconImage.sprite = Node.icon;
                iconImage.enabled = Node.icon != null;
            }

            if (rankText != null)
            {
                bool showRank = Node.maxRank > 1;
                rankText.gameObject.SetActive(showRank);
                if (showRank) rankText.text = $"{rank}/{Node.maxRank}";
            }

            if (costText != null)
            {
                costText.gameObject.SetActive(!maxed);
                if (!maxed) costText.text = $"[{Node.GetNextCost(rank):0} Grace]";
            }

            bool glow = !maxed && runtime.CanAfford(Node);
            if (buyableGlow != null) buyableGlow.SetActive(glow);
        }

        private void ApplySilhouetteVisual()
        {
            // Not literal invisibility - the shape itself still renders (per the explicit spec,
            // "greyed out... just a shape") so the player can see the web's overall structure. What
            // disappears is content: no rank, no cost, no interaction.
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (shapeImage != null) shapeImage.color = SilhouetteColor;
            if (iconImage != null) iconImage.enabled = false;
            if (rankText != null) rankText.gameObject.SetActive(false);
            if (costText != null) costText.gameObject.SetActive(false);
            if (buyableGlow != null) buyableGlow.SetActive(false);
        }

        private void Update()
        {
            if (buyableGlowImage == null || !buyableGlow.activeSelf) return;
            pulseTimer += Time.deltaTime;
            float a = Mathf.Lerp(0.35f, 0.9f, (Mathf.Sin(pulseTimer * 3f) + 1f) * 0.5f);
            var c = buyableGlowImage.color;
            c.a = a;
            buyableGlowImage.color = c;
        }

        public void OnPointerClick(PointerEventData eventData) => manager?.HandleNodeClicked(this);
        public void OnPointerEnter(PointerEventData eventData) => manager?.HandleNodeHoverEnter(this);
        public void OnPointerExit(PointerEventData eventData) => manager?.HandleNodeHoverExit(this);
    }
}
