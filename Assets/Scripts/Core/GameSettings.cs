using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Core
{
    public enum NumberNotation { Standard, Scientific, Engineering }

    /// <summary>
    /// Centralized, PlayerPrefs-persisted display/accessibility settings shared across every
    /// screen (number notation, font scale — future home for resolution/quality too). Static so
    /// any screen can read/write without threading a reference chain through every UI script.
    /// </summary>
    public static class GameSettings
    {
        private const string NotationKey = "ClickerGenesis.NumberNotation";
        private const string FontScaleKey = "ClickerGenesis.FontScale";
        private const string FullscreenKey = "ClickerGenesis.Fullscreen";
        private const string ResolutionIndexKey = "ClickerGenesis.ResolutionIndex";
        private const string BatterySaverKey = "ClickerGenesis.BatterySaver";
        private const string QualityLevelKey = "ClickerGenesis.QualityLevel";
        private const string RunInBackgroundKey = "ClickerGenesis.RunInBackground";
        private const string ManagerAutoBuyEnabledKey = "ClickerGenesis.ManagerAutoBuyEnabled";
        private const string ManagerAutoBuyReserveIndexKey = "ClickerGenesis.ManagerAutoBuyReserveIndex";
        private const string OrientationKey = "ClickerGenesis.Orientation";
        private const string ScrollSpeedKey = "ClickerGenesis.ScrollSpeed";

        public static event System.Action OnChanged;

        /// <summary>Player's explicit portrait/landscape choice (2026-08-09, real mobile-device
        /// layout fix). Default Auto - the game follows the device's live orientation, same as
        /// most mobile apps, until the player explicitly locks one via Settings. Read live every
        /// frame by ResponsiveCanvasController, not just at startup, so a mid-session change (or a
        /// live device rotation while on Auto) takes effect immediately.</summary>
        public static OrientationPreference Orientation
        {
            get => (OrientationPreference)PlayerPrefs.GetInt(OrientationKey, (int)OrientationPreference.Auto);
            set
            {
                PlayerPrefs.SetInt(OrientationKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        /// <summary>Unity's Screen.orientation lock only does anything meaningful on mobile - on
        /// desktop the OS window manager owns rotation, so ResponsiveCanvasController skips setting
        /// it there (still recomputes the reference resolution live either way).</summary>
        public static bool IsOrientationLockSupported =>
            Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;

        /// <summary>Real cross-platform ScrollRect.scrollSensitivity control (2026-08-09, bug #86 -
        /// the user's explicit ask after the Active Upgrades panel's own scroll-too-slow bug turned
        /// out to be a layout defect, not something this setting alone could have fixed, but a real
        /// in-game speed control was still missing entirely). One shared value applied to every
        /// scrollable list in the game (Scribes/Managers/Support, Verses/Chapters/Books, the Skill
        /// Tree's pan, Stats, Achievements, Active Upgrades) via ScrollSpeedApplier, rather than a
        /// per-screen setting - a player who finds scrolling too slow/fast almost certainly means it
        /// everywhere, not on one specific list. Default 25 matches the value hand-picked for the
        /// Active Upgrades panel fix; range clamped to keep it usable at either extreme.</summary>
        public static float ScrollSpeed
        {
            get => PlayerPrefs.GetFloat(ScrollSpeedKey, 25f);
            set
            {
                PlayerPrefs.SetFloat(ScrollSpeedKey, Mathf.Clamp(value, 5f, 80f));
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        public static NumberNotation Notation
        {
            get => (NumberNotation)PlayerPrefs.GetInt(NotationKey, (int)NumberNotation.Standard);
            set
            {
                PlayerPrefs.SetInt(NotationKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        /// <summary>1.0 = default. Clamped so text never shrinks below a readable floor
        /// (requested: nothing under ~13pt-equivalent) or grows past a usable ceiling.</summary>
        public static float FontScale
        {
            get => PlayerPrefs.GetFloat(FontScaleKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(FontScaleKey, Mathf.Clamp(value, 0.85f, 1.6f));
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Index into Screen.resolutions. -1 = use whatever the OS/device already has.
        /// Desktop-only lever - see IsResolutionSelectionSupported.</summary>
        public static int ResolutionIndex
        {
            get => PlayerPrefs.GetInt(ResolutionIndexKey, -1);
            set { PlayerPrefs.SetInt(ResolutionIndexKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>Picking an exact pixel resolution isn't a meaningful lever on mobile (the OS
        /// owns the screen size; there's no windowed mode to resize) - desktop gets the resolution
        /// cycle button, mobile gets the QualityLevel cycle button instead. See
        /// SettingsScreenUI.RefreshAll for where this switches the UI.</summary>
        public static bool IsResolutionSelectionSupported =>
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.LinuxEditor;

        /// <summary>Index into QualitySettings.names (the project's own quality tiers - currently
        /// "Mobile"/"PC" from the URP template's per-platform defaults, extendable to real
        /// Low/Medium/High tiers later without any code change here). -1 = use whatever Unity
        /// picked as the platform default (see ProjectSettings QualitySettings
        /// m_PerPlatformDefaultQuality) and never overridden by the player.
        ///
        /// This is Unity's own GPU-abstraction layer, not a per-vendor (Nvidia/AMD/mobile GPU)
        /// system - URP already runs generically across GPU vendors via the graphics driver, so
        /// there's nothing vendor-specific to add here. What varies by device is *how much* the
        /// GPU can handle, which is exactly what quality tiers control.</summary>
        public static int QualityLevel
        {
            get => PlayerPrefs.GetInt(QualityLevelKey, -1);
            set { PlayerPrefs.SetInt(QualityLevelKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>Caps frame rate and drops the quality tier - a real, immediately meaningful
        /// lever for battery/performance even before any dedicated power-profiling exists.</summary>
        public static bool BatterySaver
        {
            get => PlayerPrefs.GetInt(BatterySaverKey, 0) == 1;
            set { PlayerPrefs.SetInt(BatterySaverKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Whether passive Ink/scribe simulation keeps running while the window is
        /// unfocused (2026-08-04, real bug fix + user's explicit request for it to be an opt-in
        /// toggle, not forced on). Default true - an idle game where progress stops the moment you
        /// alt-tab defeats the point of "idle." PlayerSettings.runInBackground (build-time) must
        /// also be true for this to have any effect at all; this is the runtime opt-out on top of
        /// that build-time capability.</summary>
        public static bool RunInBackground
        {
            get => PlayerPrefs.GetInt(RunInBackgroundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(RunInBackgroundKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Application.runInBackground = value;
            }
        }

        /// <summary>Pushes all persisted display settings to the actual engine/OS state. Call once
        /// at bootstrap (GameLoopController.Awake) so they take effect even if the player never
        /// opens the Settings screen this session.</summary>
        public static void ApplyDisplaySettings()
        {
            Screen.fullScreen = Fullscreen;

            if (IsResolutionSelectionSupported)
            {
                var resolutions = Screen.resolutions;
                int index = ResolutionIndex;
                if (index >= 0 && index < resolutions.Length)
                {
                    var r = resolutions[index];
                    Screen.SetResolution(r.width, r.height, Fullscreen);
                }
            }

            int qualityIndex = QualityLevel;
            if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(qualityIndex, true);

            ApplyBatterySaver(BatterySaver);
            Application.runInBackground = RunInBackground;
        }

        public static void ApplyBatterySaver(bool enabled)
        {
            Application.targetFrameRate = enabled ? 30 : -1;
            QualitySettings.SetQualityLevel(enabled ? 0 : QualitySettings.names.Length - 1, true);
        }

        /// <summary>Whether managers auto-purchase their own scribe tier every frame once
        /// affordable (2026-08-04). Default true - matches the existing always-on behavior; this
        /// is the opt-out the user asked for after that feature shipped, same pattern as
        /// RunInBackground.</summary>
        public static bool ManagerAutoBuyEnabled
        {
            get => PlayerPrefs.GetInt(ManagerAutoBuyEnabledKey, 1) == 1;
            set { PlayerPrefs.SetInt(ManagerAutoBuyEnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Ink reserve tiers auto-buy will never spend below, so managers don't drain the
        /// wallet to zero when the player is trying to save up for something else. Index-based
        /// (not a raw float) since Ink balances run into the hundreds of millions and PlayerPrefs
        /// floats lose precision there - same reasoning as the multiplier-tier arrays elsewhere.
        /// Extended past 1M (2026-08-06), then to a fixed 1e36 ladder (2026-08-09) after a real
        /// endgame save outgrew the old 1T ceiling - both superseded same-day by this dynamic
        /// generator, per the user's explicit correction: the tier list should track the player's
        /// own real lifetime Ink earned (the same figure the Stats screen shows), not an arbitrary
        /// fixed ceiling picked in advance. Steps in a repeating [1, 10, 50, 100, 500] pattern per
        /// order-of-magnitude "decade" (0-500, then 1K-500K, then 1M-500M, ...), stopping at the
        /// highest step that does not exceed lifetimeInkEarned - e.g. a player who has earned
        /// 166Qa lifetime sees tiers only up to 100Qa (500Qa would exceed it), matching the user's
        /// own worked example exactly. Recomputed live off InkWallet.LifetimeEarned rather than
        /// cached, so the available tiers grow automatically as the player's lifetime total
        /// grows - see GameLoopController.ManagerAutoBuyReserve/CycleManagerAutoBuyReserve for the
        /// call sites that supply the live lifetimeInkEarned value.</summary>
        public static double[] GenerateReserveTiers(double lifetimeInkEarned)
        {
            var tiers = new List<double> { 0 };
            double magnitude = 1;
            double[] stepsInDecade = { 1, 10, 50, 100, 500 };

            while (magnitude <= 1e300)
            {
                bool addedAny = false;
                foreach (var step in stepsInDecade)
                {
                    double value = step * magnitude;
                    if (value > lifetimeInkEarned) return tiers.ToArray();
                    tiers.Add(value);
                    addedAny = true;
                }
                if (!addedAny) break;
                magnitude *= 1000;
            }
            return tiers.ToArray();
        }

        public static int ManagerAutoBuyReserveIndex
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(ManagerAutoBuyReserveIndexKey, 0));
            set { PlayerPrefs.SetInt(ManagerAutoBuyReserveIndexKey, value); PlayerPrefs.Save(); }
        }
    }
}
