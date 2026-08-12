using System;
using UnityEngine;
using Velopack;
using Velopack.Sources;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Opt-in desktop auto-updater (2026-08-10), built to be reusable across future projects,
    /// not just this one - the user's explicit ask. Wraps Velopack (MIT, github.com/velopack/velopack),
    /// which is one of the few update frameworks that explicitly supports apps without a
    /// conventional .NET Main() entry point (Unity, Electron, C++) rather than only classic
    /// console/WinForms/WPF apps.
    ///
    /// Deliberately OPT-IN, not silent: per the user's explicit request, this never installs an
    /// update on its own. CheckForUpdatesAsync() only reports whether one exists; the player (or
    /// tester) must press a real "Install Update" button - see SettingsScreenUI - before anything
    /// is downloaded or applied. This also means a build can be held back from a specific tester
    /// simply by not telling them to click it, without needing any server-side gating.
    ///
    /// VelopackApp.Build().Run() MUST run before any other game code, including GameLoopController's
    /// bootstrap - it's how Velopack intercepts the special command-line flags a Setup.exe passes on
    /// first install/uninstall (create shortcuts, register uninstaller, etc). RuntimeInitializeOnLoadMethod
    /// with SubsystemRegistration is the earliest hook Unity exposes, running before any scene's Awake.
    ///
    /// IMPORTANT, not yet verified: this can only be proven correct against a real `vpk`-packaged
    /// build (see the release-process notes in CLAUDE.md/Roadmap.html) - Velopack has no meaningful
    /// behavior to test from inside the Editor or a raw non-packaged Player build, since
    /// UpdateManager.IsInstalled reports false outside of a real Velopack-installed app. Treat this
    /// the same as the resolution-toggle/fullscreen bugs: Editor-verified compile-clean, NOT
    /// Editor-verified functionally correct.
    /// </summary>
    public class AppUpdateManager : MonoBehaviour
    {
        public static AppUpdateManager Instance { get; private set; }

        /// <summary>Set this to the real GitHub repo URL once the vpk release pipeline is live.</summary>
        [SerializeField] private string githubRepoUrl = "https://github.com/neogentrics/Clicker-Genesis";

        private UpdateManager velopackManager;

        public bool IsSupportedPlatform =>
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer;

        public bool IsCheckingForUpdate { get; private set; }
        public bool IsDownloading { get; private set; }
        public bool UpdateAvailable { get; private set; }
        public string AvailableVersion { get; private set; }

        /// <summary>Short state suffix only ("Up to date.", "Update check failed.", etc) - the
        /// current-version/update-found text itself is composed by the UI from CurrentVersionDisplay/
        /// UpdateAvailable/AvailableVersion directly, per the user's 2026-08-10 spec (the label should
        /// read like a version readout, not an instruction to press a button).</summary>
        public string StatusMessage { get; private set; } = "";

        /// <summary>Application.version as authored in Player Settings (ProjectSettings bundleVersion).</summary>
        public string CurrentVersionDisplay => Application.version;

        public event Action OnStateChanged;

        // Velopack's own update-check result, held so DownloadAndApplyUpdate doesn't have to re-check.
        private UpdateInfo pendingUpdateInfo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RunVelopackEarly()
        {
            // No-ops harmlessly when not launched from a real Velopack-installed app (Editor, a
            // bare unpackaged Player build, mobile). Must run before anything else touches the
            // filesystem/registry the installer manages.
            VelopackApp.Build().Run();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            if (!IsSupportedPlatform)
            {
                StatusMessage = "Auto-update is only available on the Windows/Mac desktop build.";
                return;
            }

            try
            {
                velopackManager = new UpdateManager(new GithubSource(githubRepoUrl, null, false));
            }
            catch (Exception ex)
            {
                // Real-world cause: running a raw (non-Velopack-installed) Player build on a
                // supported platform - Velopack can't find its own install metadata. Fail soft.
                StatusMessage = "Update check unavailable (not a Velopack-installed build).";
                Debug.LogWarning($"AppUpdateManager: UpdateManager init failed - {ex.Message}");
                return;
            }

            // Automatic background check (2026-08-10, user's explicit ask) - this is still only the
            // read-only "does an update exist" step, so it stays consistent with the opt-in-only rule:
            // only DownloadAndApplyUpdate() (a real player click) ever mutates the install.
            CheckForUpdates();
        }

        public async void CheckForUpdates()
        {
            if (velopackManager == null || IsCheckingForUpdate || IsDownloading) return;

            IsCheckingForUpdate = true;
            StatusMessage = "";
            OnStateChanged?.Invoke();

            try
            {
                pendingUpdateInfo = await velopackManager.CheckForUpdatesAsync();
                if (pendingUpdateInfo != null)
                {
                    UpdateAvailable = true;
                    AvailableVersion = pendingUpdateInfo.TargetFullRelease.Version.ToString();
                    StatusMessage = "";
                }
                else
                {
                    UpdateAvailable = false;
                    AvailableVersion = null;
                    StatusMessage = "Up to date.";
                }
            }
            catch (Exception ex)
            {
                // Same root cause as the Awake() constructor catch above: a raw (non-vpk-installed)
                // Player build has no Velopack install metadata for CheckForUpdatesAsync() to read,
                // so this path throws on every dev/test build, not just on a real network failure.
                // Worded to match that catch block instead of reading as a scary generic error.
                UpdateAvailable = false;
                StatusMessage = "Update check unavailable (not a packaged install).";
                Debug.LogWarning($"AppUpdateManager: check failed - {ex.Message}");
            }
            finally
            {
                IsCheckingForUpdate = false;
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>The one and only path that actually mutates the install - only ever called from
        /// a real player-clicked "Install Update" confirmation, never automatically.</summary>
        public async void DownloadAndApplyUpdate()
        {
            if (velopackManager == null || pendingUpdateInfo == null || IsDownloading) return;

            IsDownloading = true;
            StatusMessage = "Downloading update...";
            OnStateChanged?.Invoke();

            try
            {
                await velopackManager.DownloadUpdatesAsync(pendingUpdateInfo,
                    progress => { StatusMessage = $"Downloading update... {progress}%"; OnStateChanged?.Invoke(); });

                StatusMessage = "Installing and restarting...";
                OnStateChanged?.Invoke();

                // Applies the update and restarts the app - this call does not return.
                velopackManager.ApplyUpdatesAndRestart(pendingUpdateInfo);
            }
            catch (Exception ex)
            {
                IsDownloading = false;
                StatusMessage = "Update download failed - see log.";
                Debug.LogWarning($"AppUpdateManager: download/apply failed - {ex.Message}");
                OnStateChanged?.Invoke();
            }
        }
    }
}
