using System.Collections;
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
            // Real destination as of 2026-08-12 (was a permanently-disabled stub) - navigates to a
            // standalone "Coming Soon" scene, same pattern as Stats/Credits/Achievements below.
            // The button itself no longer says "(Coming Soon)" since the screen it opens does.
            if (StoreButton != null)
            {
                StoreButton.interactable = true;
                if (StoreButtonLabel != null) StoreButtonLabel.text = "Store";
                StoreButton.onClick.AddListener(OpenStore);
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
                AchievementsButton.interactable = true;
                if (AchievementsButtonLabel != null) AchievementsButtonLabel.text = "Achievements";
                AchievementsButton.onClick.AddListener(GoToAchievements);
            }

            ApplyCompactModeIfMobile();

            if (OverlayRoot != null) OverlayRoot.SetActive(false);
        }

        /// <summary>2026-08-11, user's ask: on mobile, buttons don't need room for both an icon and a
        /// label - the player can learn what each icon means, same as most mobile game HUDs. Desktop
        /// keeps icon+text. Same Application.platform gating pattern already used by
        /// GameSettings.IsResolutionSelectionSupported/IsOrientationLockSupported - not a new
        /// pattern for this project. Runs once at Awake (button layout doesn't change mid-session).</summary>
        private void ApplyCompactModeIfMobile()
        {
            bool isMobile = Application.platform == RuntimePlatform.Android ||
                             Application.platform == RuntimePlatform.IPhonePlayer;
            if (!isMobile || MainPanel == null) return;

            var panel = MainPanel.transform.Find("Panel");
            if (panel == null) return;

            foreach (Transform btn in panel)
            {
                var text = btn.Find("Text");
                var icon = btn.Find("Icon");
                if (text == null || icon == null) continue; // buttons without both stay as-is

                text.gameObject.SetActive(false);
                var iconRt = icon.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
            }
        }

        private CanvasGroup overlayCanvasGroup;
        private Coroutine openAnimCoroutine;

        public void Show()
        {
            if (OverlayRoot != null) OverlayRoot.SetActive(true);
            // A plain pause always lands on the main button list - Stats/Credits are opened
            // explicitly afterward (see OpenStats/OpenCredits), which re-hide this again.
            if (StatsPanel != null) StatsPanel.SetActive(false);
            if (CreditsPanel != null) CreditsPanel.SetActive(false);
            if (MainPanel != null) MainPanel.SetActive(true);

            // Bug #127 (2026-08-16): the overlay used to just pop onto screen instantly with no
            // transition, inconsistent with the entrance animations already added elsewhere in the
            // project. Real scale+fade-in, cheap enough to run every open (no external tween lib).
            if (OverlayRoot != null)
            {
                if (overlayCanvasGroup == null) overlayCanvasGroup = OverlayRoot.GetComponent<CanvasGroup>();
                if (overlayCanvasGroup == null) overlayCanvasGroup = OverlayRoot.AddComponent<CanvasGroup>();
                if (openAnimCoroutine != null) StopCoroutine(openAnimCoroutine);
                openAnimCoroutine = StartCoroutine(PlayOpenAnimation());
            }
        }

        private IEnumerator PlayOpenAnimation()
        {
            var rt = OverlayRoot.transform as RectTransform;
            const float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - (1f - p) * (1f - p); // ease-out
                overlayCanvasGroup.alpha = eased;
                if (rt != null) rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
                yield return null;
            }
            overlayCanvasGroup.alpha = 1f;
            if (rt != null) rt.localScale = Vector3.one;
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

        /// <summary>Real session stats (2026-08-06, in-overlay panel; REVISED 2026-08-10, Phase 6
        /// of the v2 redesign - now a real dedicated scene instead of a panel swap, so it can carry
        /// more than a single popup's worth of content). Captures which scene to return to (Pause
        /// can be opened from any of the 5 main screens) and closes the pause overlay before
        /// navigating, since StatsScreen is now a genuinely separate scene, not a panel floating
        /// inside this one.</summary>
        private string statsReturnSceneName = "ClickerScreen";

        private void OpenStats()
        {
            statsReturnSceneName = SceneManager.GetActiveScene().name;
            Hide();
            SceneManager.LoadScene("StatsScreen", LoadSceneMode.Single);
        }

        /// <summary>Consumed once by StatsScreenUI's Back button - returns to whichever scene Pause
        /// was actually opened from, defaulting to ClickerScreen if somehow unset.</summary>
        public string ConsumeStatsReturnScene()
        {
            string s = string.IsNullOrEmpty(statsReturnSceneName) ? "ClickerScreen" : statsReturnSceneName;
            statsReturnSceneName = "ClickerScreen";
            return s;
        }

        private void CloseStats()
        {
            if (StatsPanel != null) StatsPanel.SetActive(false);
            if (MainPanel != null) MainPanel.SetActive(true);
        }

        private void RefreshStats()
        {
            if (StatsContentLabel == null) return;
            StatsContentLabel.text = BuildStatsText(GameLoopController.Instance);
        }

        /// <summary>Legacy single-string version - kept only for the now-inert old in-overlay
        /// StatsPanel (OpenStats navigates to the real StatsScreen scene instead of using this
        /// panel now). Prefer BuildStatsSections for anything new.</summary>
        public static string BuildStatsText(GameLoopController c)
        {
            if (c == null) return "No active session.";
            var sb = new System.Text.StringBuilder();
            foreach (var (title, body) in BuildStatsSections(c))
                sb.Append($"<b>{title}</b>\n{body}\n\n");
            return sb.ToString();
        }

        /// <summary>Stats broken into discrete named sections (2026-08-10, real user redesign ask
        /// mid-build - "different panels... you can scroll through them" instead of one continuous
        /// text blob), matching how every other list in this project already presents content as
        /// individual bordered cards (Scribe/Manager/Support rows, achievement cards), not a single
        /// wall of text. StatsScreenUI builds one visual panel per entry.</summary>
        public static (string Title, string Body)[] BuildStatsSections(GameLoopController c)
        {
            if (c == null) return new[] { ("Stats", "No active session.") };

            // Grace ever earned isn't tracked as its own field - current balance plus everything
            // ever spent is mathematically identical and avoids a second counter that could drift
            // out of sync with the real one.
            double graceEverEarned = c.Prestige.Grace + c.Prestige.GraceEverSpent;

            string ink =
                $"Balance: {NumberFormatter.FormatWhole(c.Wallet.Balance)}\n" +
                $"Lifetime earned: {NumberFormatter.FormatWhole(c.Wallet.LifetimeEarned)}\n" +
                $"Lifetime spent: {NumberFormatter.FormatWhole(c.Wallet.TotalSpent)}";

            string grace =
                $"Balance: {NumberFormatter.FormatWhole(c.Prestige.Grace)}\n" +
                $"Lifetime earned: {NumberFormatter.FormatWhole(graceEverEarned)}\n" +
                $"Lifetime spent: {NumberFormatter.FormatWhole(c.Prestige.GraceEverSpent)}\n" +
                $"Free Prestiges: {c.Prestige.FreePrestigeCount}\n" +
                $"Reset Prestiges: {c.Prestige.ResetPrestigeCount}";

            string progress =
                $"Skills bought: {c.Skills.PurchasedNodeCount()}\n" +
                $"Managers bought: {c.Scribes.UnlockedManagerCount()}\n" +
                $"Verses unlocked: {c.NextVerseIndex}\n" +
                $"Chapters completed: {c.ChaptersCompletedCount}\n" +
                $"Books completed: {c.BooksCompletedCount}";

            string output =
                $"Clicking Power: {NumberFormatter.Format(c.EffectiveTapAmount)} Ink/tap\n" +
                $"Passive Income: {NumberFormatter.Format(c.EffectiveInkPerSecond)} Ink/sec";

            string store = "Boosts used: N/A (Store not built yet)";

            return new[]
            {
                ("Ink", ink),
                ("Grace", grace),
                ("Progress", progress),
                ("Current Output", output),
                ("Active Bonuses", BuildBonusBreakdown(c)),
                ("Store", store),
            };
        }

        /// <summary>Real current-output breakdown (2026-08-06, user's explicit ask - "shows what
        /// the current clicking power is plus all the bonuses that are being applied"). Every
        /// bonus line only appears when it's actually non-zero, matching the "don't show inert
        /// stats" pattern already used on the Managers/Scribes rows. Returns just the bonus-list
        /// body now (2026-08-10) - the section header/Current Output split moved to
        /// BuildStatsSections above.</summary>
        public static string BuildBonusBreakdown(GameLoopController c)
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

            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>2026-08-11 - Credits promoted to its own standalone scene (CreditsScreenUI),
        /// same "own scene, not a cramped overlay" move already made for Stats. Captures which
        /// scene to return to, same pattern as statsReturnSceneName/OpenStats above.</summary>
        private string creditsReturnSceneName = "MainMenu";

        private void OpenCredits()
        {
            creditsReturnSceneName = SceneManager.GetActiveScene().name;
            Hide();
            SceneManager.LoadScene("CreditsScreen", LoadSceneMode.Single);
        }

        /// <summary>Consumed once by CreditsScreenUI's Back button - returns to whichever scene
        /// Credits was actually opened from, defaulting to MainMenu if somehow unset.</summary>
        public string ConsumeCreditsReturnScene()
        {
            string s = string.IsNullOrEmpty(creditsReturnSceneName) ? "MainMenu" : creditsReturnSceneName;
            creditsReturnSceneName = "MainMenu";
            return s;
        }

        /// <summary>Main Menu's own info/Credits button calls this directly - there is no paused
        /// game to resume from the menu, so this just navigates straight to CreditsScreen with
        /// MainMenu as the return scene. Replaces the old in-overlay-panel version now that
        /// Credits is a real standalone scene.</summary>
        public void ShowCreditsStandalone()
        {
            OpenCredits();
        }

        /// <summary>Legacy - kept only for the now-inert old in-overlay CreditsPanel (OpenCredits
        /// navigates to the real CreditsScreen scene instead of using this panel now), same
        /// pattern as CloseStats/the old StatsPanel above.</summary>
        private void CloseCredits()
        {
            if (CreditsPanel != null) CreditsPanel.SetActive(false);
            if (MainPanel != null) MainPanel.SetActive(true);
        }

        public static string BuildCreditsText()
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
                "Modern GDR - Free icons pack - Jennifer Bertaggia\n" +
                "SpellBook. Preface - Rexard\n" +
                "Animal Icons 2D Pack - Ferro Entertainment\n" +
                "Skybox backdrops - AssetProviderForAll\n" +
                "Metallic GUI - kat_amirah\n" +
                "Super Pixel Effects Gigapack (light burst particles) - unTied Games\n" +
                "Retro Pixel Ribbons, Banners and Frames 2 - BDragon1727\n" +
                "\n<b>Fonts</b>\n" +
                "Ibarra Real Nova - Google Fonts / The Ibarra Real Nova Project Authors (SIL Open Font License 1.1)\n" +
                "\n<b>Music</b>\n" +
                "Piano Instrumental Loops, Medieval Vol. 2, Sci-Fi Music Pack - AlkaKrab (royalty-free, commercial use allowed, credit appreciated but not required)\n" +
                "  Tracks used: Piano Instrumental 1 (Main Menu), Piano Instrumental 8 (Credits), Medieval Vol. 2 7 (Clicker Screen), Sci-Fi 5 Loop (Achievements)\n" +
                "\n<b>Sound Effects</b>\n" +
                "Free UI Click Sound Pack - SwishSwoosh\n" +
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

        /// <summary>Same "remember which scene to return to" pattern as statsReturnSceneName /
        /// creditsReturnSceneName above.</summary>
        private string storeReturnSceneName = "ClickerScreen";

        private void OpenStore()
        {
            storeReturnSceneName = SceneManager.GetActiveScene().name;
            Hide();
            SceneManager.LoadScene("StoreScreen", LoadSceneMode.Single);
        }

        /// <summary>Consumed once by StoreScreenUI's Back button.</summary>
        public string ConsumeStoreReturnScene()
        {
            string s = string.IsNullOrEmpty(storeReturnSceneName) ? "ClickerScreen" : storeReturnSceneName;
            storeReturnSceneName = "ClickerScreen";
            return s;
        }

        private void GoToMainMenu()
        {
            Hide();
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        }

        /// <summary>Achievements is a dedicated scene, not another Pause Menu panel (2026-08-08) -
        /// deliberately so the achievement content session can build its own UI there without
        /// touching this controller, same reasoning as GoToMainMenu.</summary>
        private void GoToAchievements()
        {
            Hide();
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene("AchievementScreen");
            else
                SceneManager.LoadScene("AchievementScreen");
        }
    }
}
