using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Consolidated, tabbed Settings screen (Sound-System-Design.html retab, 2026-08-11 - supersedes
    /// the earlier single-panel layout). Four tabs - Audio/Display/Gameplay/Data - same
    /// SetTab(int)/ActiveTabColor pattern already used by ClickerScreenUI's Scribes/Managers/Support
    /// tabs and BuyVerseScreenUI's Verses/Chapters/Books tabs. Every option here is a genuinely
    /// working feature (backed by GameSettings/PlayerPrefs or the MasterMixer), not a stub.
    /// </summary>
    public class SettingsScreenUI : MonoBehaviour
    {
        private static readonly Color ActiveTabColor = new Color(0.957f, 0.925f, 0.847f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.72f, 0.65f, 0.53f, 1f);

        [Header("Tabs (0=Audio, 1=Display, 2=Gameplay, 3=Data)")]
        public Button AudioTabButton;
        public Button DisplayTabButton;
        public Button GameplayTabButton;
        public Button DataTabButton;
        public GameObject AudioTabRoot;
        public GameObject DisplayTabRoot;
        public GameObject GameplayTabRoot;
        public GameObject DataTabRoot;

        [Header("Audio - master mute")]
        public Button MasterMuteButton;
        public TMP_Text MasterMuteLabel;

        [Header("Audio - volume rows (Minus/Plus steppers, same pattern as Font Size)")]
        public Button MasterVolumeMinusButton;
        public Button MasterVolumePlusButton;
        public TMP_Text MasterVolumeLabel;
        public Button SfxVolumeMinusButton;
        public Button SfxVolumePlusButton;
        public TMP_Text SfxVolumeLabel;
        public Button MusicVolumeMinusButton;
        public Button MusicVolumePlusButton;
        public TMP_Text MusicVolumeLabel;
        public Button VoiceVolumeMinusButton;
        public Button VoiceVolumePlusButton;
        public TMP_Text VoiceVolumeLabel;

        [Header("Font size")]
        public Button FontSizeDownButton;
        public Button FontSizeUpButton;
        public TMP_Text FontSizeLabel;

        [Header("Number notation")]
        public Button NotationCycleButton;
        public TMP_Text NotationLabel;

        [Header("Font Family (2026-08-13) - real accessibility feature, cycles FontApplier.Library entries")]
        public Button FontFamilyCycleButton;
        public TMP_Text FontFamilyLabel;

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

        [Header("Scroll Speed (2026-08-09, bug #86) - real cross-platform control, applies to every scrollable list via ScrollSpeedApplier")]
        public Button ScrollSpeedCycleButton;
        public TMP_Text ScrollSpeedLabel;

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

        [Header("Check for Updates (2026-08-10, revised same day) - desktop-only" +
            " (AppUpdateManager.IsSupportedPlatform). ONE button that relabels itself: reads" +
            " 'Check for Updates' normally (also the manual re-check the user still gets if the" +
            " automatic background check found nothing, or hasn't run yet), 'Install' once an update" +
            " is found. The label shows the current version and, when relevant, the found version -" +
            " it's a version readout, not an instruction. Still opt-in for the download/install step" +
            " itself: clicking only downloads+installs when the button is already in Install mode -" +
            " nothing happens automatically beyond the read-only check. Lives on the Data tab.")]
        public GameObject UpdateSectionRoot;
        public Button UpdateActionButton;
        public TMP_Text UpdateActionButtonLabel;
        public TMP_Text UpdateStatusLabel;

        // Computed once in Awake (desktop-only gate) - SetTab combines this with "is Data tab active"
        // so UpdateSectionRoot is never shown on mobile even while the Data tab itself is open.
        private bool updatesSupported;

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

            if (AudioTabButton != null) AudioTabButton.onClick.AddListener(() => SetTab(0));
            if (DisplayTabButton != null) DisplayTabButton.onClick.AddListener(() => SetTab(1));
            if (GameplayTabButton != null) GameplayTabButton.onClick.AddListener(() => SetTab(2));
            if (DataTabButton != null) DataTabButton.onClick.AddListener(() => SetTab(3));

            if (MasterMuteButton != null) MasterMuteButton.onClick.AddListener(ToggleMasterMute);
            if (MasterVolumeMinusButton != null) MasterVolumeMinusButton.onClick.AddListener(() => { GameSettings.MasterVolume -= 0.1f; RefreshMasterVolume(); });
            if (MasterVolumePlusButton != null) MasterVolumePlusButton.onClick.AddListener(() => { GameSettings.MasterVolume += 0.1f; RefreshMasterVolume(); });
            if (SfxVolumeMinusButton != null) SfxVolumeMinusButton.onClick.AddListener(() => { GameSettings.SfxVolume -= 0.1f; RefreshSfxVolume(); });
            if (SfxVolumePlusButton != null) SfxVolumePlusButton.onClick.AddListener(() => { GameSettings.SfxVolume += 0.1f; RefreshSfxVolume(); });
            if (MusicVolumeMinusButton != null) MusicVolumeMinusButton.onClick.AddListener(() => { GameSettings.MusicVolume -= 0.1f; RefreshMusicVolume(); });
            if (MusicVolumePlusButton != null) MusicVolumePlusButton.onClick.AddListener(() => { GameSettings.MusicVolume += 0.1f; RefreshMusicVolume(); });
            if (VoiceVolumeMinusButton != null) VoiceVolumeMinusButton.onClick.AddListener(() => { GameSettings.VoiceVolume -= 0.1f; RefreshVoiceVolume(); });
            if (VoiceVolumePlusButton != null) VoiceVolumePlusButton.onClick.AddListener(() => { GameSettings.VoiceVolume += 0.1f; RefreshVoiceVolume(); });

            if (FontSizeDownButton != null) FontSizeDownButton.onClick.AddListener(() => AdjustFontScale(-0.1f));
            if (FontSizeUpButton != null) FontSizeUpButton.onClick.AddListener(() => AdjustFontScale(0.1f));
            if (NotationCycleButton != null) NotationCycleButton.onClick.AddListener(CycleNotation);
            if (FontFamilyCycleButton != null) FontFamilyCycleButton.onClick.AddListener(CycleFontFamily);
            if (FullscreenToggleButton != null) FullscreenToggleButton.onClick.AddListener(ToggleFullscreen);
            if (BatterySaverToggleButton != null) BatterySaverToggleButton.onClick.AddListener(ToggleBatterySaver);
            if (RunInBackgroundToggleButton != null) RunInBackgroundToggleButton.onClick.AddListener(ToggleRunInBackground);
            // Landscape-only lock (2026-08-12) - orientation is no longer a player-facing choice
            // (locked at the OS level via Player Settings), so this row is always hidden now,
            // not just on desktop.
            if (OrientationCycleButton != null) OrientationCycleButton.onClick.AddListener(CycleOrientation);
            OrientationCycleButton?.gameObject.SetActive(false);
            OrientationLabel?.gameObject.SetActive(false);
            if (ScrollSpeedCycleButton != null) ScrollSpeedCycleButton.onClick.AddListener(CycleScrollSpeed);
            if (DeleteSaveButton != null) DeleteSaveButton.onClick.AddListener(ShowDeleteConfirm);
            if (DeleteConfirmYesButton != null) DeleteConfirmYesButton.onClick.AddListener(HandleDeleteConfirmYes);
            if (DeleteConfirmNoButton != null) DeleteConfirmNoButton.onClick.AddListener(HandleDeleteConfirmNo);
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(false);

            updatesSupported = AppUpdateManager.Instance != null && AppUpdateManager.Instance.IsSupportedPlatform;
            if (updatesSupported)
            {
                if (UpdateActionButton != null) UpdateActionButton.onClick.AddListener(HandleUpdateAction);
                AppUpdateManager.Instance.OnStateChanged += RefreshUpdateStatus;
                RefreshUpdateStatus();
            }

            // Same button/label slot does double duty: exact resolution picking only makes sense
            // on desktop (windowed mode, arbitrary monitor sizes) - mobile has no windowing to
            // resize, so it gets a Quality tier cycle instead. See GameSettings.
            if (ResolutionCycleButton != null)
                ResolutionCycleButton.onClick.AddListener(GameSettings.IsResolutionSelectionSupported ? CycleResolution : CycleQuality);
            if (!GameSettings.IsResolutionSelectionSupported) FullscreenToggleButton?.gameObject.SetActive(false);

            SetTab(0);
            RefreshAll();
        }

        /// <summary>Same pattern as ClickerScreenUI/BuyVerseScreenUI's SetTab - toggles which tab
        /// root is active and tints the tab buttons' own Image (they ARE their background, no
        /// separate background object). UpdateSectionRoot lives outside the four tab roots (it's a
        /// non-scrolling section docked at the bottom of Panel, see SettingsScreen.unity) so it
        /// gets its own combined visibility check here rather than being a normal tab-root child.</summary>
        private void SetTab(int tab)
        {
            if (AudioTabRoot != null) AudioTabRoot.SetActive(tab == 0);
            if (DisplayTabRoot != null) DisplayTabRoot.SetActive(tab == 1);
            if (GameplayTabRoot != null) GameplayTabRoot.SetActive(tab == 2);
            if (DataTabRoot != null) DataTabRoot.SetActive(tab == 3);
            if (UpdateSectionRoot != null) UpdateSectionRoot.SetActive(tab == 3 && updatesSupported);

            SetTabButtonColor(AudioTabButton, tab == 0);
            SetTabButtonColor(DisplayTabButton, tab == 1);
            SetTabButtonColor(GameplayTabButton, tab == 2);
            SetTabButtonColor(DataTabButton, tab == 3);
        }

        private static void SetTabButtonColor(Button button, bool active)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = active ? ActiveTabColor : InactiveTabColor;
        }

        // ---- Audio tab ----

        private void ToggleMasterMute()
        {
            GameSettings.MasterMuted = !GameSettings.MasterMuted;
            RefreshMasterMute();
        }

        private void RefreshMasterMute()
        {
            if (MasterMuteLabel != null) MasterMuteLabel.text = "Mute";
            SetToggleButtonText(MasterMuteButton, GameSettings.MasterMuted);
        }

        private static readonly Color ToggleOnColor = new Color(0.30f, 0.85f, 0.35f);
        private static readonly Color ToggleOffColor = new Color(0.85f, 0.25f, 0.22f);

        /// <summary>These toggle buttons (Mute/Fullscreen/Battery Saver/Run in Background) read as
        /// plain unlabeled squares next to a separate "X: On/Off" text row - user's explicit
        /// correction (2026-08-12): if clicking the button is what flips the state, the ON/OFF
        /// state belongs ON the button (like a real checkbox), not off to the side on inert row
        /// text. The row label now just names the setting; the button itself shows and color-codes
        /// the current state via its own TMP_Text child - green ON / red OFF (same day, second
        /// correction: green-vs-muted-brown wasn't a clear enough OFF signal), plus a real glow
        /// (UI Outline component, same technique as the Skill Tree's owned-node glow) so the state
        /// still reads even if the text color alone is hard to make out.</summary>
        private static void SetToggleButtonText(Button button, bool on)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = on ? "ON" : "OFF";
                text.color = on ? ToggleOnColor : ToggleOffColor;
            }

            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = on ? new Color(ToggleOnColor.r, ToggleOnColor.g, ToggleOnColor.b, 0.85f) : new Color(0f, 0f, 0f, 0f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = on;
        }

        private void RefreshMasterVolume()
        {
            if (MasterVolumeLabel != null) MasterVolumeLabel.text = $"Master Volume: {GameSettings.MasterVolume * 100f:F0}%";
        }

        private void RefreshSfxVolume()
        {
            if (SfxVolumeLabel != null) SfxVolumeLabel.text = $"SFX Volume: {GameSettings.SfxVolume * 100f:F0}%";
        }

        private void RefreshMusicVolume()
        {
            if (MusicVolumeLabel != null) MusicVolumeLabel.text = $"Music Volume: {GameSettings.MusicVolume * 100f:F0}%";
        }

        private void RefreshVoiceVolume()
        {
            if (VoiceVolumeLabel != null) VoiceVolumeLabel.text = $"Voice Volume: {GameSettings.VoiceVolume * 100f:F0}%";
        }

        // ---- Display / Gameplay / Data (unchanged from the pre-retab single-panel build) ----

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

        /// <summary>Plain (non-color-coded) button-text setter, for cycle buttons that select
        /// between named options rather than toggling on/off - user's explicit correction
        /// (2026-08-12): the button should show which option it's about to select ("Scientific",
        /// "Normal"), not a generic "Change"/duplicate of the row's own label.</summary>
        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = value;
        }

        private void CycleFontFamily()
        {
            var library = FontApplier.Instance != null ? FontApplier.Instance.Library : null;
            if (library == null || library.Entries == null || library.Entries.Length == 0) return;
            GameSettings.FontChoice = (GameSettings.FontChoice + 1) % library.Entries.Length;
            RefreshFontFamily();
        }

        private void RefreshFontFamily()
        {
            if (FontFamilyLabel != null) FontFamilyLabel.text = "Font Family";
            var library = FontApplier.Instance != null ? FontApplier.Instance.Library : null;
            if (library == null || library.Entries == null || library.Entries.Length == 0) return;
            int index = Mathf.Clamp(GameSettings.FontChoice, 0, library.Entries.Length - 1);
            SetButtonText(FontFamilyCycleButton, library.Entries[index].DisplayName);
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

        // Slow/Normal/Fast/Very Fast - concrete scrollSensitivity values a player can cycle
        // through without needing to understand what "sensitivity" means numerically. 25 (Normal)
        // matches the value hand-picked for the Active Upgrades panel fix (bug #79), so a player
        // who never touches this setting gets that same baseline everywhere.
        private static readonly (string label, float value)[] ScrollSpeedSteps =
        {
            ("Slow", 12f), ("Normal", 25f), ("Fast", 40f), ("Very Fast", 60f),
        };

        private void CycleScrollSpeed()
        {
            int current = 0;
            for (int i = 0; i < ScrollSpeedSteps.Length; i++)
                if (Mathf.Approximately(ScrollSpeedSteps[i].value, GameSettings.ScrollSpeed)) { current = i; break; }
            GameSettings.ScrollSpeed = ScrollSpeedSteps[(current + 1) % ScrollSpeedSteps.Length].value;
            RefreshScrollSpeed();
        }

        private void RefreshScrollSpeed()
        {
            if (ScrollSpeedLabel != null) ScrollSpeedLabel.text = "Scroll Speed";
            string label = "Normal";
            foreach (var step in ScrollSpeedSteps)
                if (Mathf.Approximately(step.value, GameSettings.ScrollSpeed)) { label = step.label; break; }
            SetButtonText(ScrollSpeedCycleButton, label);
        }

        private void RefreshAll()
        {
            RefreshMasterMute();
            RefreshMasterVolume();
            RefreshSfxVolume();
            RefreshMusicVolume();
            RefreshVoiceVolume();
            RefreshFontSize();
            RefreshNotation();
            RefreshFontFamily();
            RefreshFullscreen();
            RefreshResolution();
            RefreshBatterySaver();
            RefreshRunInBackground();
            RefreshOrientation();
            RefreshScrollSpeed();
        }

        private void RefreshFontSize()
        {
            if (FontSizeLabel != null) FontSizeLabel.text = $"Font Size: {GameSettings.FontScale * 100f:F0}%";
        }

        private void RefreshNotation()
        {
            double example = 1234567.0;
            if (NotationLabel != null) NotationLabel.text = $"Number Format\n(e.g. {NumberFormatter.Format(example)})";
            SetButtonText(NotationCycleButton, GameSettings.Notation.ToString());
        }

        private void RefreshFullscreen()
        {
            if (FullscreenLabel != null) FullscreenLabel.text = "Fullscreen";
            SetToggleButtonText(FullscreenToggleButton, GameSettings.Fullscreen);
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
                BatterySaverLabel.text = GameSettings.BatterySaver ? "Battery Saver (30fps)" : "Battery Saver";
            SetToggleButtonText(BatterySaverToggleButton, GameSettings.BatterySaver);
        }

        private void RefreshRunInBackground()
        {
            if (RunInBackgroundLabel != null) RunInBackgroundLabel.text = "Run in Background";
            SetToggleButtonText(RunInBackgroundToggleButton, GameSettings.RunInBackground);
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

        private void OnDestroy()
        {
            if (AppUpdateManager.Instance != null) AppUpdateManager.Instance.OnStateChanged -= RefreshUpdateStatus;
        }

        /// <summary>The button does double duty by design (2026-08-10 revision) - if an update is
        /// already known (mgr.UpdateAvailable), this click is the real "download and install"
        /// confirmation; otherwise it's just a manual re-check. Nothing downloads/installs unless
        /// the button was already showing "Install" when clicked - the opt-in rule still holds.</summary>
        private void HandleUpdateAction()
        {
            var mgr = AppUpdateManager.Instance;
            if (mgr == null) return;
            if (mgr.UpdateAvailable) mgr.DownloadAndApplyUpdate();
            else mgr.CheckForUpdates();
        }

        private void RefreshUpdateStatus()
        {
            var mgr = AppUpdateManager.Instance;
            if (mgr == null) return;

            if (UpdateStatusLabel != null)
            {
                string text = $"Version: v{mgr.CurrentVersionDisplay} Beta";
                if (mgr.UpdateAvailable) text += $" (Update Found: v{mgr.AvailableVersion} Beta)";
                else if (mgr.IsCheckingForUpdate) text += " (Checking for updates...)";
                else if (!string.IsNullOrEmpty(mgr.StatusMessage)) text += $" ({mgr.StatusMessage})";
                UpdateStatusLabel.text = text;
            }

            if (UpdateActionButtonLabel != null)
            {
                UpdateActionButtonLabel.text = mgr.IsDownloading ? "Installing..."
                    : mgr.IsCheckingForUpdate ? "Checking..."
                    : mgr.UpdateAvailable ? "Install"
                    : "Check for Updates";
            }

            if (UpdateActionButton != null) UpdateActionButton.interactable = !mgr.IsCheckingForUpdate && !mgr.IsDownloading;
        }
    }
}
