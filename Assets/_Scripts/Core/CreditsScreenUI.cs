using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real dedicated Credits scene (2026-08-11), promoted off the old small Pause Menu popup -
    /// same "own scene, not a cramped overlay" pattern as StatsScreenUI. Reuses
    /// PauseMenuController.BuildCreditsText() for content instead of duplicating it. Auto-scrolls
    /// slowly downward (user's explicit ask) so the credits read like real end-credits rather than
    /// requiring the player to scroll manually - a manual drag still works and simply overrides the
    /// auto-scroll for that frame, same as any typical auto-scrolling credits screen.
    /// </summary>
    public class CreditsScreenUI : MonoBehaviour
    {
        public TMP_Text Body;
        public ScrollRect ScrollView;
        public Button BackButton;

        [Tooltip("Normalized scroll units per second (0-1 range). Small on purpose - readable, not a wall-of-text blur.")]
        public float AutoScrollSpeed = 0.02f;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (BackButton != null) BackButton.onClick.AddListener(GoBack);
            if (Body != null) Body.text = PauseMenuController.BuildCreditsText();
            if (ScrollView != null) ScrollView.verticalNormalizedPosition = 1f; // start at top
        }

        private void Update()
        {
            if (ScrollView == null) return;
            // Only auto-scroll while nothing is actively dragging the view - otherwise the two
            // fight and the credits jitter under the player's own finger/mouse.
            if (ScrollView.velocity.sqrMagnitude > 0.01f) return;
            float next = ScrollView.verticalNormalizedPosition - AutoScrollSpeed * Time.deltaTime;
            ScrollView.verticalNormalizedPosition = Mathf.Clamp01(next);
        }

        private void GoBack()
        {
            string target = PauseMenuController.Instance != null ? PauseMenuController.Instance.ConsumeCreditsReturnScene() : "MainMenu";
            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }
    }
}
