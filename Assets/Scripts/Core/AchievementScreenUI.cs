using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClickerGenesis.Achievements;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Achievement list screen (2026-08-08, redesigned 2026-08-09 per user reference screenshots -
    /// Bloons TD6 / Kingdom Rush / Cookie Clicker style achievement browsers). Card grid instead of
    /// a flat single-column list, category tabs, and a search field - the same "browse hundreds of
    /// entries" problem those games solve, which this project is heading toward once the full
    /// 656-achievement design (see Achievement-System-Design.html) gets built out past the current
    /// 64-entry KJV headline set.
    ///
    /// Deliberately does NOT use the Skill Tree's shared-description-box pattern the handoff note
    /// suggested - that pattern exists because Skill Tree nodes are small circles with no room for
    /// text. These cards have real estate for their own description, same as every reference
    /// screenshot and same as this project's own ScribeListUI/ManagerListUI/SupportListUI rows,
    /// which already show description text inline plus a HoverTooltip for anything truncated.
    /// Matches established project convention instead of introducing a new mechanism.
    ///
    /// Cards are built ONCE (the achievement set is fixed at scene-load time, unlike scribe rosters
    /// which change per active book) - Refresh() only updates existing TMP/Image values on
    /// OnStateChanged, never Instantiates/Destroys, so no bug-#22-class per-frame rebuild risk and
    /// no PermanentUpgradesListUI-style change-detection hash is needed here.
    /// </summary>
    public class AchievementScreenUI : MonoBehaviour
    {
        private class Card
        {
            public AchievementDefinition Def;
            public GameObject Root;
            public Image CardBackground;
            public Image Icon;
            public Image Frame;
            public TMP_Text NameText;
            public TMP_Text DescText;
            public TMP_Text ProgressLabel;
            public Image ProgressFill;
            public HoverTooltip Tooltip;
        }

        [Header("Header")]
        public TMP_Text HeaderText;
        public TMP_InputField SearchInput;
        public Button BackButton;

        [Header("Category tabs (generated at runtime)")]
        public Transform TabContainer;
        public GameObject TabButtonTemplate;

        [Header("Card grid")]
        public Transform Content;
        public GameObject CardTemplate;

        /// <summary>Diamond rank frames from the MetallicUI itch.io pack (2026-08-09, user's
        /// explicit ask - "gold ones are the really hard ones... bronze, well-to-do ones"). Now
        /// wired to the real AchievementDefinition.tier field (added 2026-08-09) instead of a
        /// hardcoded GoldFrame placeholder - see FrameFor().</summary>
        [Header("Rank frames (diamond, MetallicUI pack)")]
        public Sprite GoldFrame;
        public Sprite SilverFrame;
        public Sprite CopperFrame;

        private Sprite FrameFor(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Gold: return GoldFrame;
                case AchievementTier.Silver: return SilverFrame != null ? SilverFrame : GoldFrame;
                default: return CopperFrame != null ? CopperFrame : GoldFrame;
            }
        }

        /// <summary>Metallic tab-button body (2026-08-09, user's explicit ask - "they're just flat
        /// squares... beautify the buttons"). Same MetallicUI pack as the rank frames, reused here
        /// instead of the flat solid-color Image the tabs used before.</summary>
        [Header("Tab button skin")]
        public Sprite TabBackgroundSprite;

        /// <summary>Real per-category icon art (2026-08-09, user's explicit ask - "check the
        /// project folder itself... before we do that" - sourced from Kyrise's RPG Icon Pack,
        /// already imported into the project and unused, rather than downloading a new pack. The
        /// generic MetallicUIKit "achievement" trophy/medal look the user was offered didn't fit
        /// this project's warm-parchment theme; these are literal object icons matching the
        /// existing Scribe/Manager row convention instead. One icon per category (not per
        /// individual achievement - impractical at 656-achievement scale) - falls back to the
        /// procedural diamond + category tint color (see CategoryFallbackColor) for categories with
        /// no assigned sprite yet (Minigame/Meta/Secret - nothing built there).</summary>
        [Header("Category icons (Kyrise RPG Icon Pack)")]
        public Sprite BookProgressIcon;
        public Sprite ChapterProgressIcon;
        public Sprite ScribeEconomyIcon;
        public Sprite ManagersIcon;
        public Sprite PrestigeIcon;
        public Sprite LevelingIcon;

        private Sprite CategoryIconSprite(AchievementCategory cat)
        {
            switch (cat)
            {
                case AchievementCategory.BookProgress: return BookProgressIcon;
                case AchievementCategory.ChapterProgress: return ChapterProgressIcon;
                case AchievementCategory.ScribeEconomy: return ScribeEconomyIcon;
                case AchievementCategory.Managers: return ManagersIcon;
                case AchievementCategory.Prestige: return PrestigeIcon;
                case AchievementCategory.Leveling: return LevelingIcon;
                default: return null;
            }
        }

        /// <summary>Copy for a spoiler-locked achievement (2026-08-09, user's explicit correction -
        /// "it shouldn't have a description, it should just say continue playing the game... the
        /// achievement title should say hidden achievement until the requirements are met"). Real
        /// per-achievement hidden/spoiler flagging is still owned by the parallel Achievement-system
        /// session; this is just the display copy for whatever they flag as spoiler=true.</summary>
        private const string HiddenAchievementName = "Hidden Achievement";
        private const string HiddenAchievementDesc = "Continue playing the game.";

        private static readonly Color UnlockedCardColor = new Color(1f, 0.92f, 0.7f, 1f);
        private static readonly Color LockedCardColor = new Color(0.85f, 0.8f, 0.72f, 1f);
        private static readonly Color TabActiveColor = new Color(0.85f, 0.65f, 0.25f, 1f);
        private static readonly Color TabInactiveColor = new Color(0.55f, 0.45f, 0.32f, 1f);

        private readonly List<Card> cards = new List<Card>();
        private readonly List<Button> tabButtons = new List<Button>();
        private AchievementCategory? activeCategory; // null = "All" or "Unlocked" (see unlockedOnlyFilter)
        private bool unlockedOnlyFilter; // true only while the "Unlocked" tab is active
        private string searchText = "";

        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (BackButton != null) BackButton.onClick.AddListener(GoBack);
            if (SearchInput != null) SearchInput.onValueChanged.AddListener(OnSearchChanged);

            BuildTabs();
            BuildCards();
            ApplyFilter();
            Refresh();

            if (Controller != null) Controller.OnStateChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void GoBack()
        {
            if (SceneTransitioner.Instance != null) SceneTransitioner.Instance.LoadScene("ClickerScreen");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("ClickerScreen");
        }

        private void OnSearchChanged(string value)
        {
            searchText = (value ?? "").Trim();
            ApplyFilter();
        }

        private List<AchievementDefinition> OrderedDefinitions() =>
            Controller.Achievements.AllDefinitions
                .OrderBy(d => d.category)
                .ThenBy(d => d.displayName)
                .ToList();

        /// <summary>One tab per category that actually has at least one achievement, plus "All"
        /// first - categories with zero entries (e.g. Minigame, Meta - nothing built there yet)
        /// don't get a dead tab. Generated at runtime from real data rather than hand-placing 9
        /// tab buttons in the scene, same reasoning as every other data-driven UI in this project.</summary>
        private void BuildTabs()
        {
            if (Controller?.Achievements == null || TabContainer == null || TabButtonTemplate == null) return;

            var categoriesPresent = Controller.Achievements.AllDefinitions
                .Select(d => d.category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            AddTab("All", null);
            AddUnlockedTab();
            foreach (var cat in categoriesPresent)
                AddTab(SplitCategoryName(cat), cat);

            SetActiveTab(0);
        }

        /// <summary>"Achieved" filter (2026-08-09, user's explicit ask - "there's no button for
        /// unlocked achievements already"; renamed from "Unlocked" to "Achieved" 2026-08-09 per the
        /// user's follow-up correction to match the project's own "Achieved"/"Achievements"
        /// terminology) - orthogonal to category, so it gets its own tab rather than being folded
        /// into the category enum.</summary>
        private void AddUnlockedTab()
        {
            var tabGo = Instantiate(TabButtonTemplate, TabContainer);
            tabGo.SetActive(true);
            tabGo.name = "Tab_Achieved";

            var text = tabGo.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = "Achieved";

            var button = tabGo.GetComponent<Button>();
            int index = tabButtons.Count;
            tabButtons.Add(button);
            button.onClick.AddListener(() =>
            {
                activeCategory = null;
                unlockedOnlyFilter = true;
                SetActiveTab(index);
                ApplyFilter();
            });
        }

        private static string SplitCategoryName(AchievementCategory cat)
        {
            // "BookProgress" -> "Book Progress" - simple insert-space-before-capital, avoids
            // hand-maintaining a separate display-name table just for tab labels.
            string name = cat.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) sb.Append(' ');
                sb.Append(name[i]);
            }
            return sb.ToString();
        }

        private void AddTab(string label, AchievementCategory? category)
        {
            var tabGo = Instantiate(TabButtonTemplate, TabContainer);
            tabGo.SetActive(true);
            tabGo.name = "Tab_" + label;

            var text = tabGo.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;

            var button = tabGo.GetComponent<Button>();
            int index = tabButtons.Count;
            tabButtons.Add(button);
            button.onClick.AddListener(() =>
            {
                activeCategory = category;
                unlockedOnlyFilter = false;
                SetActiveTab(index);
                ApplyFilter();
            });
        }

        private void SetActiveTab(int index)
        {
            for (int i = 0; i < tabButtons.Count; i++)
            {
                var img = tabButtons[i].GetComponent<Image>();
                if (img == null) continue;
                // Metallic tab body (2026-08-09) instead of a flat solid-color square - active vs
                // inactive is still a tint on the same shaped sprite, just no longer a bare rect.
                if (TabBackgroundSprite != null && img.sprite != TabBackgroundSprite)
                {
                    img.sprite = TabBackgroundSprite;
                    img.type = Image.Type.Simple;
                }
                img.color = i == index ? TabActiveColor : TabInactiveColor;
            }
        }

        private void BuildCards()
        {
            if (Controller?.Achievements == null || Content == null || CardTemplate == null) return;

            foreach (var def in OrderedDefinitions())
            {
                var cardGo = Instantiate(CardTemplate, Content);
                cardGo.SetActive(true);
                cardGo.name = "AchievementCard_" + def.id;

                var icon = cardGo.transform.Find("Icon")?.GetComponent<Image>();

                // Diamond icon slot (2026-08-09, user's explicit ask - "get rid of the original
                // squares and replace them with diamonds too... it'll look cooler and unique").
                // The flat square Image becomes a procedurally-shaped diamond fill (reusing the
                // Skill Tree's shape-sprite generator - same diamond silhouette, no new art needed
                // for the fill), then the real MetallicUI diamond frame sits on top of it for the
                // ornate metal border. Both layers are diamond-shaped now, not a square underneath
                // a diamond-shaped border.
                Image frame = null;
                if (icon != null)
                {
                    icon.sprite = NodeShapeSprites.Get(Progression.SkillNodeShape.Diamond);
                    icon.type = Image.Type.Simple;

                    var iconRt = icon.GetComponent<RectTransform>();
                    var slotSize = iconRt.sizeDelta; // the full icon "slot" before any shrinking below

                    var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    frameGo.transform.SetParent(icon.transform.parent, false);
                    var frameRt = frameGo.GetComponent<RectTransform>();
                    frameRt.anchorMin = iconRt.anchorMin;
                    frameRt.anchorMax = iconRt.anchorMax;
                    frameRt.pivot = iconRt.pivot;
                    var frameSize = slotSize + new Vector2(24f, 24f);
                    frameRt.sizeDelta = frameSize;
                    // Pivot-aware centering (2026-08-09 real bug, user report: "the diamond frame
                    // doesn't match up with the diamond behind it") - Icon uses a left-edge pivot
                    // (0, 0.5), same as every other row icon in this project (Scribe/Manager/Support
                    // rows). Naively copying anchoredPosition while growing sizeDelta symmetrically
                    // only expands the non-pivot side, visibly shifting the frame off-center - the
                    // exact same bug class already caught and fixed once for the Manager/Support
                    // icon badges (see IconBadge centering note). Same general formula reused here.
                    frameRt.anchoredPosition = iconRt.anchoredPosition - (frameSize - slotSize) * (new Vector2(0.5f, 0.5f) - iconRt.pivot);
                    frame = frameGo.GetComponent<Image>();
                    frame.sprite = FrameFor(def.tier);
                    frame.type = Image.Type.Simple;
                    frame.raycastTarget = false;
                    frameGo.transform.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);

                    // Shrink the icon itself well below the frame's own bounds (2026-08-09, user
                    // report: "the icons... don't fit inside the diamonds" - MetallicUIKit's diamond
                    // frame art has a thick ornate bezel, so its actual see-through opening is much
                    // smaller than the frame sprite's full bounding box; a same-size icon pokes its
                    // square corners out past the visible diamond window at all four points). Same
                    // pivot-aware centering formula as the frame above, just shrinking inward instead
                    // of growing outward, so the icon stays centered in the original slot.
                    var iconSize = slotSize * 0.56f;
                    iconRt.sizeDelta = iconSize;
                    iconRt.anchoredPosition -= (iconSize - slotSize) * (new Vector2(0.5f, 0.5f) - iconRt.pivot);
                }

                var card = new Card
                {
                    Def = def,
                    Root = cardGo,
                    CardBackground = cardGo.GetComponent<Image>(),
                    Icon = icon,
                    Frame = frame,
                    NameText = cardGo.transform.Find("Name")?.GetComponent<TMP_Text>(),
                    DescText = cardGo.transform.Find("Description")?.GetComponent<TMP_Text>(),
                    ProgressLabel = cardGo.transform.Find("ProgressBar/ProgressLabel")?.GetComponent<TMP_Text>(),
                    ProgressFill = cardGo.transform.Find("ProgressBar/Fill")?.GetComponent<Image>(),
                    Tooltip = cardGo.GetComponent<HoverTooltip>()
                };
                cards.Add(card);
            }
        }

        /// <summary>Shows/hides cards per the active category tab + search text. Search matches
        /// against the DISPLAYED text (reveal-aware) - a spoiler-locked achievement is searchable
        /// only by its "???" placeholder, never its real hidden name, so search can't be used to
        /// leak spoilers.</summary>
        private void ApplyFilter()
        {
            if (Controller?.Achievements == null) return;

            foreach (var card in cards)
            {
                bool unlocked = Controller.Achievements.IsUnlocked(card.Def.id);
                bool categoryMatch = unlockedOnlyFilter
                    ? unlocked
                    : (activeCategory == null || card.Def.category == activeCategory.Value);

                bool reveal = unlocked || !card.Def.spoiler;
                string visibleName = reveal ? card.Def.displayName : HiddenAchievementName;
                string visibleDesc = reveal ? card.Def.description : HiddenAchievementDesc;

                bool searchMatch = string.IsNullOrEmpty(searchText) ||
                    visibleName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    visibleDesc.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                card.Root.SetActive(categoryMatch && searchMatch);
            }
        }

        private void Refresh()
        {
            if (Controller?.Achievements == null) return;

            if (HeaderText != null)
                HeaderText.text = $"{Controller.Achievements.GetAchievedCount()} / {Controller.Achievements.TotalCount} " +
                                   $"({Controller.Achievements.GetAchievedPercentage():F0}%)";

            foreach (var card in cards)
            {
                var def = card.Def;
                bool unlocked = Controller.Achievements.IsUnlocked(def.id);
                bool reveal = unlocked || !def.spoiler;

                string name = reveal ? def.displayName : HiddenAchievementName;
                string desc = reveal ? def.description : HiddenAchievementDesc;

                if (card.NameText != null) card.NameText.text = name;
                if (card.DescText != null) card.DescText.text = desc;
                if (card.Tooltip != null) card.Tooltip.Text = desc;

                if (card.CardBackground != null)
                    card.CardBackground.color = unlocked ? UnlockedCardColor : LockedCardColor;

                if (def.type == AchievementType.Progress)
                {
                    float progress = Controller.Achievements.GetProgress(def.id);
                    float goal = Mathf.Max(1f, def.progressGoal);
                    SetFill(card.ProgressFill, Mathf.Clamp01(progress / goal));
                    if (card.ProgressLabel != null)
                        card.ProgressLabel.text = reveal ? $"{progress:N0} / {goal:N0}" : HiddenAchievementDesc;
                }
                else
                {
                    SetFill(card.ProgressFill, unlocked ? 1f : 0f);
                    if (card.ProgressLabel != null) card.ProgressLabel.text = unlocked ? "Unlocked" : "Locked";
                }

                // Real per-category icon art where available (2026-08-09), else the procedural
                // diamond shape tinted by category as before.
                if (card.Icon != null)
                {
                    var realIcon = CategoryIconSprite(def.category);
                    if (realIcon != null)
                    {
                        card.Icon.sprite = realIcon;
                        card.Icon.color = Color.white;
                    }
                    else
                    {
                        card.Icon.sprite = NodeShapeSprites.Get(Progression.SkillNodeShape.Diamond);
                        card.Icon.color = CategoryFallbackColor(def.category);
                    }
                }
            }
        }

        /// <summary>Resizes a progress bar's Fill by growing its RectTransform's right anchor
        /// (anchorMin.x stays 0, anchorMax.x = fraction) instead of Image.fillAmount - matches the
        /// exact technique the real XP/Book/OT triple-lane bar uses (ClickerScreenUI.SetLane), which
        /// is why THAT bar looks like a clean rounded pill and this one didn't (2026-08-09, user's
        /// explicit correction: "I wanted the progress bars on these to look the same as the XP
        /// bars... it doesn't look like this will work properly"). Image.Type.Filled clips a 9-sliced
        /// sprite's own mesh, which cuts straight through the rounded-corner border art and produces
        /// a flat/square edge at any fraction other than 0 or 1 - Sliced type + anchor-driven width
        /// keeps the whole 9-slice border intact at every fraction, since the sprite is never
        /// geometry-clipped, only resized.</summary>
        private static void SetFill(Image fill, float fraction)
        {
            if (fill == null) return;
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        /// <summary>Solid-color fallback swatch per category for achievements with no real icon
        /// assigned yet - same "blank/placeholder rather than forced" rule already used for
        /// scribe/manager icons elsewhere, until real per-achievement icons are sourced.</summary>
        private static Color CategoryFallbackColor(AchievementCategory cat)
        {
            switch (cat)
            {
                case AchievementCategory.BookProgress: return new Color(0.55f, 0.35f, 0.2f);
                case AchievementCategory.ChapterProgress: return new Color(0.4f, 0.5f, 0.3f);
                case AchievementCategory.ScribeEconomy: return new Color(0.75f, 0.6f, 0.25f);
                case AchievementCategory.Managers: return new Color(0.35f, 0.4f, 0.6f);
                case AchievementCategory.Prestige: return new Color(0.6f, 0.35f, 0.65f);
                case AchievementCategory.Leveling: return new Color(0.3f, 0.55f, 0.55f);
                case AchievementCategory.Minigame: return new Color(0.7f, 0.4f, 0.4f);
                case AchievementCategory.Meta: return new Color(0.85f, 0.75f, 0.2f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
}
