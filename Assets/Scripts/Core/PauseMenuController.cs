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

        [Header("Credits panel (2026-08-07) - real third-party asset attribution, built now that pre-releases go out to testers")]
        public Button CreditsButton;
        public GameObject CreditsPanel;
        public TMP_Text CreditsContentLabel;
        public Button CreditsBackButton;

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
            if (CreditsButton != null) CreditsButton.onClick.AddListener(OpenCredits);
            if (CreditsBackButton != null) CreditsBackButton.onClick.AddListener(CloseCredits);
            if (CreditsPanel != null) CreditsPanel.SetActive(false);
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
                BuildBonusBreakdown(c) +
                $"\n<b>Store</b>\n" +
                $"Boosts used: N/A (Store not built yet)";
        }

        /// <summary>Real current-output breakdown (2026-08-06, user's explicit ask - "shows what
        /// the current clicking power is plus all the bonuses that are being applied"). Every
        /// bonus line only appears when it's actually non-zero, matching the "don't show inert
        /// stats" pattern already used on the Managers/Scribes rows.</summary>
        private static string BuildBonusBreakdown(GameLoopController c)
        {
            double clickPowerBoost = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ClickPowerMultiplier);
            double incomeBoost = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.IncomeMultiplier);
            double managerBonusBoost = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ManagerBonusBoost);
            double progressBoost = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ProgressMultiplierBoost);
            double milestoneBoost = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ScribeMilestoneBoost);
            double graceGainBonus = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.GraceGainBonus);
            double pricingDiscount = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.PricingDiscount);
            double managerLevelDiscount = c.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ManagerUnlockLevelDiscount);
            int resetCount = c.Prestige.ResetPrestigeCount;
            double resetBaseBonus = resetCount * 0.5; // mirrors GameLoopController.ResetBaseInkPerSecondPerReset
            double bookCompletionMultiplier = (c.BooksCompletedCount > 0 && resetCount > 0) ? 1 + resetCount : 1.0;

            var sb = new System.Text.StringBuilder();
            sb.Append("\n<b>Current Output</b>\n");
            sb.Append($"Clicking Power: {NumberFormatter.Format(c.EffectiveTapAmount)} Ink/tap\n");
            sb.Append($"Passive Income: {NumberFormatter.Format(c.EffectiveInkPerSecond)} Ink/sec\n");

            sb.Append("\n<b>Active Bonuses</b>\n");
            bool any = false;
            if (clickPowerBoost > 0) { sb.Append($"Click Power (skills): +{clickPowerBoost * 100:F0}%\n"); any = true; }
            if (incomeBoost > 0) { sb.Append($"Ink Income (skills): +{incomeBoost * 100:F0}%\n"); any = true; }
            if (managerBonusBoost > 0) { sb.Append($"Manager Bonus (Overseer's Wisdom): +{managerBonusBoost * 100:F0}%\n"); any = true; }
            if (progressBoost > 0) { sb.Append($"Progress Multiplier boost (skills): +{progressBoost * 100:F0}%\n"); any = true; }
            if (milestoneBoost > 0) { sb.Append($"Milestone Bonus (Scribe's Diligence): +{milestoneBoost * 100:F0}%\n"); any = true; }
            if (graceGainBonus > 0) { sb.Append($"Grace Gain (skills): +{graceGainBonus * 100:F0}%\n"); any = true; }
            if (pricingDiscount > 0) { sb.Append($"Verse/Chapter Pricing (Swift Unlock): -{pricingDiscount * 100:F1}%\n"); any = true; }
            if (managerLevelDiscount > 0) { sb.Append($"Manager Unlock Level (skills): -{managerLevelDiscount:F0} levels\n"); any = true; }
            sb.Append($"Progress Multiplier (verse/chapter buys): ×{c.ProgressMultiplier:F2}\n"); any = true;
            if (resetBaseBonus > 0) { sb.Append($"Base Ink/sec from Resets: +{resetBaseBonus:F1}/sec ({resetCount} reset{(resetCount == 1 ? "" : "s")})\n"); any = true; }
            if (bookCompletionMultiplier > 1.0) { sb.Append($"Book Completion Multiplier: ×{bookCompletionMultiplier:F0}\n"); any = true; }
            if (!any) sb.Append("None yet.\n");

            return sb.ToString();
        }

        // Set when Credits was opened directly from the Main Menu's own info button (no paused
        // game to return to), rather than via the in-game Pause overlay's Credits button - tells
        // CloseCredits() whether "back" means "show the pause list again" or "close entirely".
        private bool _creditsOpenedStandalone;

        /// <summary>Real third-party attribution (2026-08-07) - the user's explicit call that
        /// credits can no longer wait for a later build now that pre-release testers are
        /// downloading real GitHub releases. Static text, not data-driven - this list only grows
        /// when a new asset actually gets wired into something, same "log it when it's used, not
        /// when it's imported" rule as the CLAUDE.md Assets &amp; Credits tracking section.</summary>
        private void OpenCredits()
        {
            if (CreditsContentLabel != null) CreditsContentLabel.text = BuildCreditsText();
            if (MainPanel != null) MainPanel.SetActive(false);
            if (CreditsPanel != null) CreditsPanel.SetActive(true);
        }

        /// <summary>Main Menu's own info/Credits button calls this directly - there is no paused
        /// game to resume from the menu, so this opens the overlay straight into the Credits
        /// panel (skipping the Resume/Settings/Stats list) and closes it entirely on Back,
        /// instead of falling through to that list. Replaces the old ComingSoonButton stub
        /// (2026-08-07) now that real Credits content exists.</summary>
        public void ShowCreditsStandalone()
        {
            _creditsOpenedStandalone = true;
            Show();
            OpenCredits();
        }

        private void CloseCredits()
        {
            if (CreditsPanel != null) CreditsPanel.SetActive(false);
            if (_creditsOpenedStandalone)
            {
                _creditsOpenedStandalone = false;
                Hide();
            }
            else if (MainPanel != null)
            {
                MainPanel.SetActive(true);
            }
        }

        private static string BuildCreditsText()
        {
            return
                "<b>Scripture</b>\n" +
                "King James Version (KJV) - public domain.\n" +
                "\n<b>Art & UI</b>\n" +
                "Fantasy Wooden GUI Free - Black Hammer\n" +
                "UI button pack 2 / UI button pack 3 - RR Studio\n" +
                "Icon packs (scroll, journal) - Homeless\n" +
                "40 Free Skill/Ability Icons Volume 1 - CaptainCatSparrow\n" +
                "FREE - RPG Fantasy Spell Icons - Blink\n" +
                "RPG Item Icons - Concept Hamster\n" +
                "Modern RPG - Free icons pack - JenniferBertaggia\n" +
                "SpellBook. Preface - Rexard\n" +
                "Animal Icons 2D Pack - Ferro Entertainment\n" +
                "Skybox backdrops - AssetProviderForAll\n" +
                "\n<b>Fonts</b>\n" +
                "Georgia / Georgia Bold - Microsoft (prototype build only - a properly licensed " +
                "serif replaces this before any commercial release)\n" +
                "\n<b>Tools</b>\n" +
                "Built with Unity, using Unity-MCP for AI-assisted development.\n" +
                "\n<i>A few recently-imported icon packs still need their publisher name " +
                "confirmed before the next release - not omitted on purpose, just not verified yet.</i>";
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
