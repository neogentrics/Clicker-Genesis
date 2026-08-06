using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Persistent singleton (spawned once from Main Menu's GameRoot, survives scene loads) that
    /// shows/hides a full-screen pause overlay from anywhere in gameplay. Replaces the two
    /// separate corner icon buttons (Settings gear + Menu hamburger) that used to live on every
    /// gameplay screen - one Pause button now opens this instead, with Settings/Main Menu/Store
    /// as options inside it (2026-08-04, explicit user request instead of continuing to cram
    /// more corner buttons onto individual screens).
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Overlay root (starts inactive)")]
        public GameObject OverlayRoot;

        [Header("Buttons")]
        public Button ResumeButton;
        public Button SettingsButton;
        public Button MainMenuButton;
        public Button StoreButton;
        public TMP_Text StoreButtonLabel;
        public Button StatsButton;
        public TMP_Text StatsButtonLabel;
        public Button AchievementsButton;
        public TMP_Text AchievementsButtonLabel;

        [Header("Stats panel (2026-08-06) - real session stats, replacing the old disabled stub")]
        public GameObject MainPanel;
        public GameObject StatsPanel;
        public TMP_Text StatsContentLabel;
        public Button StatsBackButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            // Same reasoning as every other screen's Awake-not-Start wiring in this project - see
            // ClickerScreenUI.Awake for the full explanation.
            if (ResumeButton != null) ResumeButton.onClick.AddListener(Hide);
            if (SettingsButton != null) SettingsButton.onClick.AddListener(OpenSettings);
            if (MainMenuButton != null) MainMenuButton.onClick.AddListener(GoToMainMenu);
            if (StoreButton != null)
            {
                StoreButton.interactable = false;
                if (StoreButtonLabel != null) StoreButtonLabel.text = "Store (Coming Soon)";
            }
            // Achievements - explicitly deferred system (see CLAUDE.md "later development" note),
            // but the user wants the button present now as a placeholder for where it'll live once
            // built, same "Coming Soon" pattern as Store. Stats is real now (2026-08-06) - see below.
            if (StatsButton != null)
            {
                StatsButton.interactable = true;
                if (StatsButtonLabel != null) StatsButtonLabel.text = "Stats";
                StatsButton.onClick.AddListener(OpenStats);
            }
            if (StatsBackButton != null) StatsBackButton.onClick.AddListener(CloseStats);
            if (StatsPanel != null) StatsPanel.SetActive(false);
            if (AchievementsButton != null)
            {
                AchievementsButton.interactable = false;
                if (AchievementsButtonLabel != null) AchievementsButtonLabel.text = "Achievements (Coming Soon)";
            }

            if (OverlayRoot != null) OverlayRoot.SetActive(false);
        }

        public void Show()
        {
            if (OverlayRoot != null) OverlayRoot.SetActive(true);
        }

        public void Hide()
        {
            if (OverlayRoot != null) OverlayRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (OverlayRoot == null) return;
            if (OverlayRoot.activeSelf) Hide();
            else Show();
        }

        /// <summary>Real session stats (2026-08-06), replacing the old disabled "Stats (Coming
        /// Soon)" stub - swaps the main button list for a read-only stats panel over the same
        /// footprint, same overlay, rather than a separate scene.</summary>
        private void OpenStats()
        {
            RefreshStats();
            if (MainPanel != null) MainPanel.SetActive(false);
            if (StatsPanel != null) StatsPanel.SetActive(true);
        }

        private void CloseStats()
        {
            if (StatsPanel != null) StatsPanel.SetActive(false);
            if (MainPanel != null) MainPanel.SetActive(true);
        }

        private void RefreshStats()
        {
            if (StatsContentLabel == null) return;
            var c = GameLoopController.Instance;
            if (c == null)
            {
                StatsContentLabel.text = "No active session.";
                return;
            }

            // Grace ever earned isn't tracked as its own field - current balance plus everything
            // ever spent is mathematically identical and avoids a second counter that could drift
            // out of sync with the real one.
            double graceEverEarned = c.Prestige.Grace + c.Prestige.GraceEverSpent;

            StatsContentLabel.text =
                $"<b>Ink</b>\n" +
                $"Balance: {NumberFormatter.FormatWhole(c.Wallet.Balance)}\n" +
                $"Lifetime earned: {NumberFormatter.FormatWhole(c.Wallet.LifetimeEarned)}\n" +
                $"Lifetime spent: {NumberFormatter.FormatWhole(c.Wallet.TotalSpent)}\n" +
                $"\n<b>Grace</b>\n" +
                $"Balance: {NumberFormatter.FormatWhole(c.Prestige.Grace)}\n" +
                $"Lifetime earned: {NumberFormatter.FormatWhole(graceEverEarned)}\n" +
                $"Lifetime spent: {NumberFormatter.FormatWhole(c.Prestige.GraceEverSpent)}\n" +
                $"Free Prestiges: {c.Prestige.FreePrestigeCount}\n" +
                $"Reset Prestiges: {c.Prestige.ResetPrestigeCount}\n" +
                $"\n<b>Progress</b>\n" +
                $"Skills bought: {c.Skills.PurchasedNodeCount()}\n" +
                $"Managers bought: {c.Scribes.UnlockedManagerCount()}\n" +
                $"Verses unlocked: {c.NextVerseIndex}\n" +
                $"Chapters completed: {c.ChaptersCompletedCount}\n" +
                $"Books completed: {c.BooksCompletedCount}\n" +
                $"\n<b>Store</b>\n" +
                $"Boosts used: N/A (Store not built yet)";
        }

        private void OpenSettings()
        {
            Hide();
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.RecordSettingsReturnScene(SceneManager.GetActiveScene().name);
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene("SettingsScreen");
            else
                SceneManager.LoadScene("SettingsScreen");
        }

        private void GoToMainMenu()
        {
            Hide();
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        }
    }
}
