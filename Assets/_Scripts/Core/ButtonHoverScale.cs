using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Universal "this button is highlighted" feedback via a slight scale-up on hover, added
    /// broadly across every button (2026-08-06 real bug fix). Root cause of the original report
    /// ("only Scribes/Managers tab buttons light up on hover"): Button.transition=ColorTint's
    /// highlightedColor is a MULTIPLICATIVE tint on the button's own Image.color - a no-op once
    /// that color is already at/near (1,1,1,1), which is true for most of this project's
    /// wood-sprite buttons (their look comes from the sprite artwork itself, tinted plain white,
    /// not from Image.color). Multiplying 1.0 by anything &gt;1.0 clamps right back to 1.0 - no
    /// visible change. Only the couple of buttons with real tint headroom below 1.0 (a near-black
    /// icon tint, an explicitly script-driven tab-active color) ever responded. Scale is visible
    /// regardless of the button's base tint, so it fixes every button uniformly without having to
    /// retune ColorTint values per-sprite.
    /// </summary>
    public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float HoverScale = 1.06f;

        private RectTransform rt;
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            rt = transform as RectTransform;
            if (rt != null) baseScale = rt.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (rt != null) rt.localScale = baseScale * HoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (rt != null) rt.localScale = baseScale;
        }
    }
}
