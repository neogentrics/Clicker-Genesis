using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// One shared tooltip box per scene, always rendered as the LAST sibling directly under
    /// Canvas so it draws on top of everything else - fixes a real bug (2026-08-05) where
    /// per-button tooltip children rendered BEHIND later UI siblings (e.g. the Tap button's
    /// tooltip drew behind the XP bar above it), because Unity UI draws depth-first: a child's
    /// draw order is entirely determined by its parent's position in ITS OWN parent's sibling
    /// list, not by a global z-index. Parenting each tooltip to its own button trapped it at that
    /// button's draw position. HoverTooltip components ask this singleton to show/hide instead of
    /// toggling a local child.
    /// </summary>
    public class TooltipOverlay : MonoBehaviour
    {
        public static TooltipOverlay Instance { get; private set; }

        public RectTransform Box;
        public TMP_Text Label;

        private void Awake()
        {
            Instance = this;
            transform.SetAsLastSibling();
            if (Box != null) Box.gameObject.SetActive(false);
        }

        /// <summary>Shows the tooltip positioned relative to anchorTarget per direction - offset
        /// far enough that it never overlaps anchorTarget itself or sits where a click would land,
        /// per the explicit "off to the side, not behind/on top of a clickable thing" requirement.</summary>
        public void Show(string text, RectTransform anchorTarget, HoverTooltip.Direction direction)
        {
            if (Box == null || Label == null || anchorTarget == null) return;

            transform.SetAsLastSibling();
            Label.text = text;
            Box.gameObject.SetActive(true);

            // Force a layout pass so Box.rect reflects the new text's content-fitted size before
            // we use it to compute an offset - otherwise the very first show of a longer tooltip
            // uses a stale (usually zero) size and mispositions.
            LayoutRebuilder.ForceRebuildLayoutImmediate(Box);

            Vector3[] corners = new Vector3[4];
            anchorTarget.GetWorldCorners(corners);
            Vector3 targetCenter = (corners[0] + corners[2]) / 2f;
            Vector2 targetSize = corners[2] - corners[0];

            float gap = 12f;
            Vector2 boxSize = Box.rect.size * Box.lossyScale.x;
            Vector2 offset;
            switch (direction)
            {
                case HoverTooltip.Direction.Left:
                    offset = new Vector2(-(targetSize.x / 2f + boxSize.x / 2f + gap), 0f);
                    break;
                case HoverTooltip.Direction.Right:
                    offset = new Vector2(targetSize.x / 2f + boxSize.x / 2f + gap, 0f);
                    break;
                case HoverTooltip.Direction.Down:
                    offset = new Vector2(0f, -(targetSize.y / 2f + boxSize.y / 2f + gap));
                    break;
                default: // Up
                    offset = new Vector2(0f, targetSize.y / 2f + boxSize.y / 2f + gap);
                    break;
            }

            Vector3 worldPos = targetCenter + (Vector3)offset;
            var canvas = GetComponentInParent<Canvas>();
            var canvasRt = canvas.transform as RectTransform;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPos);
            Box.anchoredPosition = localPos;
        }

        public void Hide()
        {
            if (Box != null) Box.gameObject.SetActive(false);
        }
    }
}
