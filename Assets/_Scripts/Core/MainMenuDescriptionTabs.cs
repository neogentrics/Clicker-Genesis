using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// About/Changelog tab switcher for the Main Menu's right-side description parchment
    /// (2026-08-08, task #177 - the one concretely-specified piece of the Main Menu redesign ask:
    /// "It's still missing the tab buttons on the top to switch between that and the change log
    /// for the current version, which should have been implemented already with a scroll
    /// feature"). About is the existing static game-description text; Changelog is new, real
    /// version-history content (not placeholder), scrollable since it will only grow.
    /// </summary>
    public class MainMenuDescriptionTabs : MonoBehaviour
    {
        public Button AboutTabButton;
        public Button ChangelogTabButton;
        public GameObject AboutPanel;
        public GameObject ChangelogPanel;
        public TMP_Text ChangelogContentLabel;

        private void Awake()
        {
            if (AboutTabButton != null) AboutTabButton.onClick.AddListener(() => SetTab(0));
            if (ChangelogTabButton != null) ChangelogTabButton.onClick.AddListener(() => SetTab(1));
            if (ChangelogContentLabel != null) ChangelogContentLabel.text = BuildChangelogText();
            SetTab(0);
        }

        private void SetTab(int tab)
        {
            if (AboutPanel != null) AboutPanel.SetActive(tab == 0);
            if (ChangelogPanel != null) ChangelogPanel.SetActive(tab == 1);
        }

        /// <summary>Real version history pulled from this project's own release notes - short
        /// highlight per version, matching the GitHub release-title convention (not a full
        /// changelog dump), newest first.</summary>
        private static string BuildChangelogText()
        {
            return
                "<b>v0.4.6 Beta</b>\n" +
                "Locked the app to landscape orientation (portrait couldn't be supported in the " +
                "same scenes without breaking landscape). Settings toggles now show ON/OFF right " +
                "on the button (green/red, glowing when on) instead of separate inert text. Fixed " +
                "hard-to-read Settings text and redesigned the Number Format/Scroll Speed rows.\n\n" +
                "<b>v0.4.5 Beta</b>\n" +
                "Live playtesting fix batch: real player build pipeline fix, book-picker click " +
                "reliability, Pause Menu/Stats/Credits redesign work.\n\n" +
                "<b>v0.4.1-0.4.4 Beta</b>\n" +
                "Full 655-achievement system with real UI, Sound & Audio system (mixer, volume " +
                "controls, music zones), Grace Skill Tree V2 (real economy + Old Testament " +
                "content), first Android build, desktop auto-update support, real mobile-device " +
                "layout fixes.\n\n" +
                "<b>v0.4.0</b>\n" +
                "KJV-scoped achievement system (headline set), Settings orientation support.\n\n" +
                "<b>v0.3.3</b>\n" +
                "Managers/Support tab redesign, first Mac build.\n\n" +
                "<b>v0.3.2</b>\n" +
                "Fixed scribe/manager verse-gate regression, doc sync.\n\n" +
                "<b>v0.3.1</b>\n" +
                "Live-playtesting fix batch, submanager system, real Genesis roster.\n\n" +
                "<b>v0.3.0</b>\n" +
                "Grace Skill Tree progressive visibility, Books tab, Reset-Prestige rework, " +
                "Stats screen, GitHub Issues bug tracking.\n\n" +
                "<b>v0.2.x</b>\n" +
                "Core loop polish: Scribes/Managers tabs, Prestige + Grace Skill Tree built, " +
                "chapter bulk-buy, full KJV text loaded, first Windows build.\n\n" +
                "<b>v0.1</b>\n" +
                "Core loop prototype: tap for Ink, buy verses, XP/leveling.";
        }
    }
}
