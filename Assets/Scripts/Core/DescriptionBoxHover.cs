using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Shows Text in a fixed, always-visible description box on hover, instead of a floating
    /// tooltip that has to be aimed at precisely - built for the Grace Skill Tree's 105 small
    /// nodes (2026-08-05, user's explicit idea): "that way every time they highlight something...
    /// even if it's not able to be bought, they can see the description." Reverts to Target's
    /// idle placeholder text on pointer exit.
    /// </summary>
    public class DescriptionBoxHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text Target;
        public string Text;

        /// <summary>Set once by the screen controller (e.g. in Awake, before any hover can
        /// happen) so every node reverts to the same idle prompt - not captured lazily from
        /// whatever the box happens to be showing on first hover, which would be race-prone.</summary>
        public static string IdleText = "";

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Target != null) Target.text = Text;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Target != null) Target.text = IdleText;
        }
    }
}
