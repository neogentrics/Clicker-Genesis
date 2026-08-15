using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Per-screen SFX mute checklist (2026-08-13, real user ask on top of the existing global
    /// Master Mute - "turn off sound for certain screens" without silencing the whole game).
    /// One row per real player-facing screen, ON/OFF toggle backed by
    /// GameSettings.IsSceneSfxMuted/SetSceneSfxMuted. Same row-clone-from-template pattern family
    /// as ScribeListUI/ManagerListUI/SupportListUI, but the list here is small and fixed (11 real
    /// screens) so it's built once on first enable, not rebuilt on every refresh.
    /// </summary>
    public class SceneSfxMuteListUI : MonoBehaviour
    {
        public struct ScreenEntry
        {
            public string SceneName;
            public string DisplayName;
            public ScreenEntry(string sceneName, string displayName) { SceneName = sceneName; DisplayName = displayName; }
        }

        // Real scene name -> friendly label. Kept here (a display-only concern) rather than in
        // GameSettings, which only ever sees the raw scene name.
        public static readonly ScreenEntry[] Screens =
        {
            new ScreenEntry("MainMenu", "Main Menu"),
            new ScreenEntry("ClickerScreen", "Clicker Screen"),
            new ScreenEntry("BuyVerseScreen", "Buy Verse Screen"),
            new ScreenEntry("SettingsScreen", "Settings"),
            new ScreenEntry("PrestigeScreen", "Skill Tree"),
            new ScreenEntry("SaveSlotScreen", "Save Slots"),
            new ScreenEntry("NewGameSetupScreen", "New Game Setup"),
            new ScreenEntry("AchievementScreen", "Achievements"),
            new ScreenEntry("StatsScreen", "Stats"),
            new ScreenEntry("CreditsScreen", "Credits"),
            new ScreenEntry("StoreScreen", "Store"),
        };

        private static readonly Color ToggleOnColor = new Color(0.30f, 0.85f, 0.35f);
        private static readonly Color ToggleOffColor = new Color(0.85f, 0.25f, 0.22f);

        public Transform Content;
        public GameObject RowTemplate;

        private readonly List<(string sceneName, Button toggle)> rows = new List<(string, Button)>();
        private bool built;

        private void OnEnable()
        {
            if (!built) BuildRows();
            Refresh();
        }

        private void BuildRows()
        {
            built = true;
            if (Content == null || RowTemplate == null) return;

            foreach (var entry in Screens)
            {
                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.name = "Row_" + entry.SceneName;
                rowGo.SetActive(true);

                var nameText = rowGo.transform.Find("Name")?.GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = entry.DisplayName;

                var toggle = rowGo.transform.Find("ToggleButton")?.GetComponent<Button>();
                if (toggle == null) continue;

                string sceneName = entry.SceneName; // capture for the closure
                toggle.onClick.AddListener(() =>
                {
                    GameSettings.SetSceneSfxMuted(sceneName, !GameSettings.IsSceneSfxMuted(sceneName));
                    RefreshRow(sceneName, toggle);
                });
                rows.Add((sceneName, toggle));
            }
        }

        private void Refresh()
        {
            foreach (var (sceneName, toggle) in rows)
                RefreshRow(sceneName, toggle);
        }

        /// <summary>"Muted" reads as OFF/red (sound suppressed on that screen); everything else is
        /// ON/green - same convention as every other toggle button in Settings
        /// (SettingsScreenUI.SetToggleButtonText), duplicated locally since this list owns its own
        /// per-row Button references rather than the single named fields that helper expects.</summary>
        private void RefreshRow(string sceneName, Button toggle)
        {
            bool muted = GameSettings.IsSceneSfxMuted(sceneName);
            var text = toggle.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = muted ? "OFF" : "ON";
                text.color = muted ? ToggleOffColor : ToggleOnColor;
            }

            var outline = toggle.GetComponent<Outline>();
            if (outline == null) outline = toggle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(ToggleOnColor.r, ToggleOnColor.g, ToggleOnColor.b, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = !muted;
        }
    }
}
