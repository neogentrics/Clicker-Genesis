using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Persistent singleton (same DontDestroyOnLoad pattern as GameLoopController/AudioManager)
    /// that applies the player's chosen font (GameSettings.FontChoice, via FontLibrary) to every
    /// TMP_Text in the currently-loaded scene, live - on every scene load and every settings
    /// change, so switching fonts in Settings takes effect immediately without needing a restart.
    ///
    /// Bold-vs-regular decision per text element: TMP_Text.fontStyle already carries a Bold flag
    /// on every label the project cares about being bold (set alongside the SemiBold font swap in
    /// the 2026-08-12 Settings-readability pass, and everywhere else bold was ever used). Reusing
    /// that existing signal means no new per-object tagging is needed across 9+ scenes - if a
    /// chosen font family has no true bold face (Roboto/Liberation Sans in this project), TMP's
    /// own algorithmic bold renders on top of the Regular asset, same as before real bold weights
    /// existed anywhere in the project.
    /// </summary>
    public class FontApplier : MonoBehaviour
    {
        public static FontApplier Instance { get; private set; }

        public FontLibrary Library;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameSettings.OnChanged += ApplyToActiveScene;
            ApplyToActiveScene();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameSettings.OnChanged -= ApplyToActiveScene;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToActiveScene();

        public void ApplyToActiveScene()
        {
            if (Library == null || Library.Entries == null || Library.Entries.Length == 0) return;

            int index = Mathf.Clamp(GameSettings.FontChoice, 0, Library.Entries.Length - 1);
            var entry = Library.Entries[index];
            if (entry?.Regular == null) return;

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var texts = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    bool wantsBold = text.fontStyle.HasFlag(FontStyles.Bold);
                    text.font = (wantsBold && entry.Bold != null) ? entry.Bold : entry.Regular;
                }
            }
        }
    }
}
