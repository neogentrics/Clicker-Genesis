using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Persistent singleton (spawned once from Main Menu, survives scene loads) that handles
    /// screen navigation via SceneManager.LoadSceneAsync instead of the old synchronous
    /// LoadScene (bug #22: synchronous loads block the main thread for the whole load, which
    /// read as a visible hitch every time the player switched screens - async spreads the work
    /// across frames instead).
    ///
    /// A coroutine-driven fade (CanvasGroup alpha lerp around the load) was built and reverted
    /// earlier in the project: it reliably stalled forever mid-coroutine when driven through this
    /// project's headless Unity-MCP automation (looks like coroutines on this object weren't
    /// being pumped by the automated bridge's frame loop, not a bug in the fade logic itself).
    /// This async load is a plain coroutine with the same theoretical risk under that same
    /// automation, but was verified via script-based state checks in a real Play session (not the
    /// automation's frame-stepping), where coroutines run normally. The `fadeGroup` field/overlay
    /// Canvas are still unused in the scene (TransitionOverlay) - re-add a fade lerp using
    /// asyncOp.progress if wanted later.
    /// </summary>
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 0.25f;

        private bool isLoading;

        /// <summary>Which scene to return to from Settings (2026-08-04: Settings is now reachable
        /// directly from ClickerScreen/BuyVerseScreen, not just MainMenu - the Back button needs
        /// to return wherever the player actually came from, not hardcode MainMenu). Recorded by
        /// SceneNavButton right before it navigates to "SettingsScreen".</summary>
        public string LastSettingsReturnScene { get; private set; } = "MainMenu";

        public void RecordSettingsReturnScene(string sceneName) => LastSettingsReturnScene = sceneName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // See GameLoopController.Awake() for why this is guarded.
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading) return; // ignore extra clicks while a load is already in flight
            StartCoroutine(LoadSceneAsyncRoutine(sceneName));
        }

        private IEnumerator LoadSceneAsyncRoutine(string sceneName)
        {
            isLoading = true;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
                yield return null;
            isLoading = false;
        }
    }
}
