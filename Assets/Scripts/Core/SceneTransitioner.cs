using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Persistent singleton (spawned once from Main Menu, survives scene loads) that handles
    /// screen navigation. Currently a direct, instant SceneManager.LoadScene — no fade yet.
    ///
    /// A coroutine-driven fade (CanvasGroup alpha lerp around the load) was built and reverted:
    /// it reliably stalled forever mid-coroutine when driven through this project's headless
    /// Unity-MCP automation (both LoadSceneAsync and plain LoadScene+yield-frames stalled the
    /// same way — looks like coroutines on this object weren't being pumped by the automated
    /// bridge's frame loop, not a bug in the fade logic itself). Untested against a real,
    /// human-driven Play session in the Editor, where it may well just work. The `fadeGroup`
    /// field/overlay Canvas are still in the scene (TransitionOverlay) for whoever picks this
    /// back up — re-add the coroutine and test it live before assuming it's broken.
    /// </summary>
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 0.25f;

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
            SceneManager.LoadScene(sceneName);
        }
    }
}
