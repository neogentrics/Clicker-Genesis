using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Consolidated Settings screen: sound (was SoundSettingsUI, folded in here), font size,
    /// number notation, fullscreen, resolution, and a battery-saver toggle. Every option here is
    /// a genuinely working feature (backed by GameSettings/PlayerPrefs), not a stub - display
    /// settings apply immediately via Screen/QualitySettings, not just cosmetically.
    /// </summary>
    public class SettingsScreenUI : MonoBehaviour
    {
        private const string SoundPrefKey = "ClickerGenesis.SoundEnabled";

        [Header("Sound")]
        public Button SoundToggleButton;
        public TMP_Text SoundToggleLabel;
        public Image SoundIconImage;
        public Sprite SoundOnIcon;
        public Sprite SoundOffIcon;

        [Header("Font size")]
        public Button FontSizeDownButton;
        public Button FontSizeUpButton;
        public TMP_Text FontSizeLabel;

        [Header("Number notation")]
        public Button NotationCycleButton;
        public TMP_Text NotationLabel;

        [Header("Fullscreen")]
        public Button FullscreenToggleButton;
        public TMP_Text FullscreenLabel;

        [Header("Resolution (desktop) / Quality (mobile) - same button slot, see Awake")]
        public Button ResolutionCycleButton;
        public TMP_Text ResolutionLabel;

        [Header("Battery saver")]
        public Button BatterySaverToggleButton;
        public TMP_Text BatterySaverLabel;

        [Header("Run in background (2026-08-04) - opt-in, off by default is a real bug: idle progress stopped entirely when unfocused")]
        public Button RunInBackgroundToggleButton;
        public TMP_Text RunInBackgroundLabel;

        [Header("Orientation (2026-08-09, task #25) - mobile-only lever, hidden on desktop where the OS window manager owns rotation")]
        public Button OrientationCycleButton;
        public TMP_Text OrientationLabel;

        [Header("Back to Game (2026-08-04) - only shown when reached from actual gameplay, not from MainMenu")]
        public GameObject BackToGameButton;

        [Header("Delete Saved Game (2026-08-08) - full reset. Explicit user ask: red/white button" +
            " so it doesn't read as a normal safe toggle, plus a Yes/No confirmation (green No/" +
            "safe, red Yes/danger) before anything actually happens.")]
        public Button DeleteSaveButton;
        public GameObject DeleteConfirmPanel;
        public TMP_Text DeleteConfirmMessageLabel;
        public Button DeleteConfirmYesButton;
        public Button DeleteConfirmNoButton;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            // "Back to Game" only makes sense if the player actually came from a gameplay screen -
            // reaching Settings directly from MainMenu means there's no in-progress game to return
            // to, so only the (separate, always-present) Menu button is meaningful there.
            if (BackToGameButton != null)
            {
                bool cameFromGameplay = SceneTransitioner.Instance != null && SceneTransitioner.Instance.LastSettingsReturnScene != "MainMenu";
                BackToGameButton.SetActive(cameFromGameplay);
            }

            if (SoundToggleButton != null) SoundToggleButton.onClick.AddListener(ToggleSound);
            if (FontSizeDownButton != null) FontSizeDownButton.onClick.AddListener(() => AdjustFontScale(-0.1f));
            if (FontSizeUpButton != null) FontSizeUpButton.onClick.AddListener(() => AdjustFontScale(0.1f));
            if (NotationCycleButton != null) NotationCycleButton.onClick.AddListener(CycleNotation);
            if (FullscreenToggleButton != null) FullscreenToggleButton.onClick.AddListener(ToggleFullscreen);
            if (BatterySaverToggleButton != null) BatterySaverToggleButton.onClick.AddListener(ToggleBatterySaver);
            if (RunInBackgroundToggleButton != null) RunInBackgroundToggleButton.onClick.AddListener(ToggleRunInBackground);
            if (OrientationCycleButton != null) OrientationCycleButton.onClick.AddListener(CycleOrientation);
            if (!GameSettings.IsOrientationLockSupported) OrientationCycleButton?.gameObject.SetActive(false);
            if (DeleteSaveButton != null) DeleteSaveButton.onClick.AddListener(ShowDeleteConfirm);
            if (DeleteConfirmYesButton != null) DeleteConfirmYesButton.onClick.AddListener(HandleDeleteConfirmYes);
            if (DeleteConfirmNoButton != null) DeleteConfirmNoButton.onClick.AddListener(HandleDeleteConfirmNo);
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(false);

            // Same button/label slot does double duty: exact resolution picking only makes sense
            // on desktop (windowed mode, arbitrary monitor sizes) - mobile has no windowing to
            // resize, so it gets a Quality tier cycle instead. See GameSettings.
            if (ResolutionCycleButton != null)
                ResolutionCycleButton.onClick.AddListener(GameSettings.IsResolutionSelectionSupported ? CycleResolution : CycleQuality);
            if (!GameSettings.IsResolutionSelectionSupported) FullscreenToggleButton?.gameObject.SetActive(false);

            RefreshAll();
        }

        private static bool IsSoundEnabled() => PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;

        private void ToggleSound()
        {
            bool newState = !IsSoundEnabled();
            PlayerPrefs.SetInt(SoundPrefKey, newState ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = newState ? 1f : 0f;
            RefreshSound();
        }

        private void AdjustFontScale(float delta)
        {
            GameSettings.FontScale += delta;
            RefreshFontSize();
        }

        private void CycleNotation()
        {
            GameSettings.Notation = (NumberNotation)(((int)GameSettings.Notation + 1) % 3);
            RefreshNotation();
        }

        private void ToggleFullscreen()
        {
            bool newState = !GameSettings.Fullscreen;
            GameSettings.Fullscreen = newState;
            Screen.fullScreen = newState;
            RefreshFullscreen();
        }

        private Coroutine pendingResolutionApply;

        private void CycleResolution()
        {
            var resolutions = Screen.resolutions;
            if (resolutions.Length == 0) return;

            int next = GameSettings.ResolutionIndex + 1;
            if (next >= resolutions.Length) next = -1; // -1 = back to native/current
            GameSettings.ResolutionIndex = next;
            RefreshResolution();

            // Debounced: Screen.SetResolution triggers a real, non-instant OS-level window/mode
            // switch (bug #32 - "needs ~3 clicks before a visible change"). Firing it on every
            // click re-issues a brand new resize before the previous one has settled, so rapid
            // clicking looked broken even though each click's setting was correctly recorded.
            // Only the resolution still selected after the clicking stops actually gets applied.
            if (pendingResolutionApply != null) StopCoroutine(pendingResolutionApply);
            pendingResolutionApply = StartCoroutine(ApplyResolutionAfterDelay(next));
        }

        private IEnumerator ApplyResolutionAfterDelay(int index)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            pendingResolutionApply = null;

            if (index >= 0)
            {
                var resolutions = Screen.resolutions;
                if (index >= resolutions.Length) yield break;
                var r = resolutions[index];
                Screen.SetResolution(r.width, r.height, GameSettings.Fullscreen);
            }
        }

        private void CycleQuality()
        {
            var names = QualitySettings.names;
            if (names.Length == 0) return;

            int next = GameSettings.QualityLevel + 1;
            if (next >= names.Length) next = 0; // wrap, unlike resolution's "back to native" -1
            GameSettings.QualityLevel = next;
            QualitySettings.SetQualityLevel(next, true);
            RefreshResolution();
        }

        private void ToggleBatterySaver()
        {
            bool newState = !GameSettings.BatterySaver;
            GameSettings.BatterySaver = newState;
            GameSettings.ApplyBatterySaver(newState);
            RefreshBatterySaver();
        }

        private void ToggleRunInBackground()
        {
            GameSettings.RunInBackground = !GameSettings.RunInBackground; // setter also applies Application.runInBackground
            RefreshRunInBackground();
        }

        /// <summary>Auto -> Portrait -> Landscape -> Auto. Auto is the sensible default (task #25's
        /// "auto-rotate default") - the game follows the device's live orientation like most mobile
        /// apps until the player explicitly locks one; ResponsiveCanvasController.Apply() (already
        /// built) reads GameSettings.Orientation live via GameSettings.OnChanged, so this takes
        /// effect immediately with no extra wiring here.</summary>
        private void CycleOrientation()
        {
            GameSettings.Orientation = (OrientationPreference)(((int)GameSettings.Orientation + 1) % 3);
            RefreshOrientation();
        }

        private void RefreshOrientation()
        {
            if (OrientationLabel == null) return;
            OrientationLabel.text = "Orientation: " + GameSettings.Orientation switch
            {
                OrientationPreference.Portrait => "Portrait",
                OrientationPreference.Landscape => "Landscape",
                _ => "Auto",
            };
        }

        private void RefreshAll()
        {
            RefreshSound();
            RefreshFontSize();
            RefreshNotation();
            RefreshFullscreen();
            RefreshResolution();
            RefreshBatterySaver();
            RefreshRunInBackground();
            RefreshOrientation();
        }

        private void RefreshSound()
        {
            bool enabled = IsSoundEnabled();
            if (SoundToggleLabel != null) SoundToggleLabel.text = enabled ? "Sound: On" : "Sound: Off";
            if (SoundIconImage != null) SoundIconImage.sprite = enabled ? SoundOnIcon : SoundOffIcon;
        }

        private void RefreshFontSize()
        {
            if (FontSizeLabel != null) FontSizeLabel.text = $"Font Size: {GameSettings.FontScale * 100f:F0}%";
        }

        private void RefreshNotation()
        {
            if (NotationLabel == null) return;
            double example = 1234567.0;
            NotationLabel.text = $"Number Format: {GameSettings.Notation}  (e.g. {NumberFormatter.Format(example)})";
        }

        private void RefreshFullscreen()
        {
            if (FullscreenLabel != null) FullscreenLabel.text = GameSettings.Fullscreen ? "Fullscreen: On" : "Fullscreen: Off";
        }

        private void RefreshResolution()
        {
            if (ResolutionLabel == null) return;

            if (!GameSettings.IsResolutionSelectionSupported)
            {
                var names = QualitySettings.names;
                int qIndex = GameSettings.QualityLevel;
                string qName = (qIndex >= 0 && qIndex < names.Length) ? names[qIndex] : QualitySettings.names[QualitySettings.GetQualityLevel()];
                ResolutionLabel.text = $"Quality: {qName}";
                return;
            }

            int index = GameSettings.ResolutionIndex;
            var resolutions = Screen.resolutions;
            if (index < 0 || index >= resolutions.Length)
                ResolutionLabel.text = $"Resolution: Native ({Screen.width}x{Screen.height})";
            else
                ResolutionLabel.text = $"Resolution: {resolutions[index].width}x{resolutions[index].height}";
        }

        private void RefreshBatterySaver()
        {
            if (BatterySaverLabel != null)
                BatterySaverLabel.text = GameSettings.BatterySaver ? "Battery Saver: On (30fps)" : "Battery Saver: Off";
        }

        private void RefreshRunInBackground()
        {
            if (RunInBackgroundLabel != null)
                RunInBackgroundLabel.text = GameSettings.RunInBackground ? "Run in Background: On" : "Run in Background: Off";
        }

        /// <summary>Opens the confirmation popup - the actual delete never happens on the first
        /// click (2026-08-08, explicit user ask: this is a deliberately rare, deliberately scary
        /// action, not a normal toggle).</summary>
        private void ShowDeleteConfirm()
        {
            if (DeleteConfirmMessageLabel != null)
                DeleteConfirmMessageLabel.text = "Are you sure you wish to delete your saved game?\nThis cannot be undone.";
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(true);
        }

        /// <summary>Deletes the save file, resets all in-memory progress to a fresh start, and
        /// returns to the Main Menu (2026-08-08, per the user's explicit spec - "brings them back
        /// to the main menu, and starts the game over again").</summary>
        private void HandleDeleteConfirmYes()
        {
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(false);

            var controller = GameLoopController.Instance;
            if (controller != null) controller.ResetGameAndDeleteSave();

            if (SceneTransitioner.Instance != null) SceneTransitioner.Instance.LoadScene("MainMenu");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        /// <summary>Just closes the popup - no save deletion, no navigation. The player stays
        /// exactly where they were (2026-08-08, per the user's explicit spec: "No" should not
        /// itself navigate anywhere, it should just cancel).</summary>
        private void HandleDeleteConfirmNo()
        {
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(false);
        }
    }
}
