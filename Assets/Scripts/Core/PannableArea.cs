using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Minimal drag-to-pan for the Skill Tree, fully independent of Unity's ScrollRect (2026-08-06
    /// rewrite - replaces the ScrollRect-based approach entirely). ScrollRect's own bounds
    /// enforcement (Clamped) and PrestigeScreenUI.Zoom()'s custom scale-based zoom kept fighting
    /// each other: Clamped recomputed and repositioned Content every frame to compensate for the
    /// new scale, which read as "scrolling instead of zooming." Switching to Unrestricted stopped
    /// that specific fight but then broke mouse-wheel zoom outright (user report, real
    /// live-testing regression) - rather than keep tuning ScrollRect settings against a use case
    /// it wasn't designed for (external code driving Content's scale), this removes ScrollRect
    /// from panning duty entirely and hand-rolls it here, so panning (drag) and zooming (scroll
    /// wheel, in PrestigeScreenUI.Update) are two fully independent systems that can never
    /// interfere with each other again.
    /// </summary>
    public class PannableArea : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform Content;

        private Canvas rootCanvas;

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (Content == null) return;
            float scale = rootCanvas != null && rootCanvas.scaleFactor > 0f ? rootCanvas.scaleFactor : 1f;
            Content.anchoredPosition += eventData.delta / scale;
        }
    }
}
