using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Desktop hover tooltip - shows Text via the scene's single TooltipOverlay instead of
    /// toggling a local child GameObject (2026-08-05, real bug fix: a tooltip parented to its own
    /// button rendered BEHIND any later-drawn sibling of that button elsewhere in the hierarchy,
    /// e.g. the Tap button's tooltip drew behind the XP bar above it - see TooltipOverlay's
    /// summary for why). Only shows on genuine mouse hover (OnPointerEnter/Exit), never on click.
    /// </summary>
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum Direction { Up, Down, Left, Right }

        [TextArea] public string Text;
        public Direction PreferredDirection = Direction.Up;

        private RectTransform rt;
        private RectTransform Rt => rt != null ? rt : (rt = GetComponent<RectTransform>());

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(Text) || TooltipOverlay.Instance == null) return;
            TooltipOverlay.Instance.Show(Text, Rt, PreferredDirection);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipOverlay.Instance != null) TooltipOverlay.Instance.Hide();
        }
    }
}
