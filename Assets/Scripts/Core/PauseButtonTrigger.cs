using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>Attach to a Button to open the persistent PauseMenuController overlay. No
    /// serialized target needed (unlike SceneNavButton) - PauseMenuController is a runtime
    /// singleton spawned from MainMenu's GameRoot, so it doesn't exist as a scene object in
    /// ClickerScreen/BuyVerseScreen at edit time; resolved via Instance at click time instead.</summary>
    [RequireComponent(typeof(Button))]
    public class PauseButtonTrigger : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                PauseMenuController.Instance?.Show();
                StartCoroutine(ResetSelectionStateNextFrame());
            });
        }

        /// <summary>The pause overlay covers this button the instant it opens, with no real mouse
        /// movement - Unity's EventSystem never sends a clean OnPointerExit, so the button's
        /// Selectable gets stuck reporting whatever visual state (Highlighted/Pressed) it was in
        /// at the moment of click until something else nudges it. Toggling the component forces
        /// Selectable's own OnEnable reset, snapping it cleanly back to Normal so the next real
        /// hover, once the overlay closes, starts fresh instead of appearing "stuck."</summary>
        private System.Collections.IEnumerator ResetSelectionStateNextFrame()
        {
            yield return null;
            if (button != null) { button.enabled = false; button.enabled = true; }
        }
    }
}
