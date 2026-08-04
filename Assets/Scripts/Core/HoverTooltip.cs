using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Minimal desktop hover tooltip for the icon-only Menu/Settings buttons (2026-08-04) - since
    /// those buttons dropped their text labels in favor of icons, this restores "what is this"
    /// context without permanently showing text. TooltipObject is expected to start inactive as a
    /// child of the button, positioned where the label should appear (e.g. above the icon).
    /// </summary>
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject TooltipObject;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TooltipObject != null) TooltipObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipObject != null) TooltipObject.SetActive(false);
        }
    }
}
