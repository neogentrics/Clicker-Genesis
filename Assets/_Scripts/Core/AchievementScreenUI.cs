using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClickerGenesis.Achievements;
using ClickerGenesis.Data;

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
            public Image CardBorder;
            public TMP_Text NameText;
            public TMP_Text DescText;
            public TMP_Text ProgressLabel;
            public Image ProgressFill;
            public HoverTooltip Tooltip;
        }

        /// <summary>One dense icon-only tile in the trophy gallery (Phase 5, 2026-08-10). No text
        /// of its own - hover updates the single shared GalleryDescriptionName/Body pair instead of
        /// a per-tile tooltip (explicit user ask - "one shared spot... otherwise we'd literally have
        /// to give each one of them a hover text little panel, and it just seems like doing too
        /// much").</summary>
        private class GalleryCard
        {
            public AchievementDefinition Def;
            public GameObject Root;
            public Image Icon;
            public Image Frame;
            public Outline HoverGlow;
        }

        [Header("Header")]
        public TMP_Text ScreenTitle;
        public TMP_Text HeaderText;
        public TMP_InputField SearchInput;
        public Button BackButton;

        /// <summary>Real user redesign (2026-08-10, supersedes the original Phase 4 "View Book
        /// Achievements" button that reloaded this scene into a separate book-only mode). The
        /// screen now DEFAULTS to the active book's scope and switches scope in place - this button
        /// is the "go wide" side of that: it flips to allMode (gameplay + every unlocked book), and
        /// is hidden while already in that mode since there's nothing left for it to do.</summary>
        public Button AllScopeButton;

        /// <summary>Real user redesign (2026-08-10) - lets the player browse ANY unlocked book's
        /// achievements, not just whichever one happens to be active in gameplay right now. Opens
        /// BookPickerPanel, same toggle/position pattern as FilterButton/FilterDropdownPanel below.
        /// Hidden entirely while allMode is on (real user correction, same day: "that button
        /// shouldn't even be visible... you're not looking at books, you're looking at all of the
        /// achievements" - redundant once you're already viewing the aggregate).</summary>
        [Header("Book picker (2026-08-10 redesign)")]
        public Button BookPickerButton;
        public GameObject BookPickerPanel;
        public Transform BookPickerContent;
        public GameObject BookPickerRowTemplate;

        [Header("Category tabs (generated at runtime)")]
        public Transform TabContainer;
        public GameObject TabButtonTemplate;

        [Header("Card grid")]
        public Transform Content;
        public GameObject CardTemplate;

        /// <summary>Real compact icon-only grid (2026-08-10, Phase 5, explicit user ask for the
        /// genuine dense Cookie-Clicker layout rather than reusing the descriptive cards above at a
        /// smaller size). A separate GridLayoutGroup/Content/CardTemplate from the main card grid -
        /// GalleryCardTemplate has no Name/Description/ProgressBar children at all, just an
        /// icon+frame, since text lives in the single shared GalleryDescriptionPanel instead.</summary>
        [Header("Trophy gallery (Phase 5, dense icon grid)")]
        public GameObject GalleryScrollView;
        public Transform GalleryContent;
        public GameObject GalleryCardTemplate;
        public GameObject NormalScrollView;
        public Button GalleryOpenButton;
        public Button GalleryShowLockedToggle;
        public TMP_Text GalleryShowLockedLabel;
        public TMP_Text GalleryDescriptionName;
        public TMP_Text GalleryDescriptionBody;

        /// <summary>Diamond rank frames from the MetallicUI itch.io pack (2026-08-09, user's
        /// explicit ask - "gold ones are the really hard ones... bronze, well-to-do ones"). Now
        /// wired to the real AchievementDefinition.tier field (added 2026-08-09) instead of a
        /// hardcoded GoldFrame placeholder - see FrameFor().</summary>
        [Header("Rank frames (diamond, MetallicUI pack)")]
        public Sprite GoldFrame;
        public Sprite SilverFrame;
        public Sprite CopperFrame;

        /// <summary>Real card-panel border (2026-08-12, user correction - the shimmer treatment
        /// was only reaching the small tier-rank diamond, not "the achievement screen" itself; this
        /// is the actual card-sized frame the user meant). A Sliced-mode ornate border sitting on
        /// top of the whole card, hollow in the middle by the source art's own design, so it frames
        /// the card's edges without ever covering the Name/Desc/ProgressBar text sitting behind it.</summary>
        [Header("Card panel border (Kenney Fantasy UI Borders)")]
        public Sprite CardBorderSprite;

        /// <summary>Real celebration on actual unlock (2026-08-12), not just a static color swap -
        /// plays at the unlocking card's own position when it's currently visible under the active
        /// scope/filter, or at BurstFallbackAnchor (meant to be the scroll Viewport's own center, so
        /// it's always on-screen) when it isn't - e.g. the achievement unlocked while the player was
        /// viewing a different book or had it filtered out.</summary>
        [Header("Unlock celebration burst")]
        public AchievementUnlockBurst UnlockBurst;
        public RectTransform BurstFallbackAnchor;

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

        /// <summary>Real user correction (2026-08-10): "if an achievement is locked, whatever icon
        /// is supposed to be there shouldn't show up... there should be a lock icon there instead
        /// until it's unlocked." The main card grid was showing the real category icon (or its
        /// procedural diamond fallback) regardless of unlock state - a real inconsistency, since the
        /// Trophy Gallery already hid the real icon behind a plain dark silhouette for locked tiles.
        /// Both views now agree: no real icon art until earned, this sprite (or a plain
        /// tinted-diamond fallback if unassigned) shows instead.</summary>
        [Header("Locked achievement icon")]
        public Sprite LockedIcon;

        /// <summary>Real user redesign (2026-08-10) - G-spot Lab's "Magic Energy" seamless swirl
        /// materials, grouped by AchievementCategory into 5 color families (per the user's own
        /// framing: "group them based off of what they are... book of the Bible, gameplay, skills")
        /// so cards sharing a category share ONE material (keeps UI batching sane at 740
        /// achievements - a unique material per individual card was explicitly ruled out). Each
        /// family has a light/dark texture pair; the user's own idea, applied here: light = earned,
        /// dark = still locked. Which texture is actually lighter differs per family (confirmed by
        /// direct pixel-luminance sampling, not assumed - Blue/Dark have _01 as the light one,
        /// Green/Orange/Purple have _02 instead), so these fields are named by role (Light/Dark),
        /// not by the source file's own numbering.</summary>
        [Header("Category card materials (G-spot Lab Magic Energy pack)")]
        public Material BlueLightMaterial;
        public Material BlueDarkMaterial;
        public Material GreenLightMaterial;
        public Material GreenDarkMaterial;
        public Material OrangeLightMaterial;
        public Material OrangeDarkMaterial;
        public Material PurpleLightMaterial;
        public Material PurpleDarkMaterial;
        public Material DarkLightMaterial;
        public Material DarkDarkMaterial;

        /// <summary>Blue = scripture-reading progress, Green = economy/growth, Purple = Prestige
        /// (matches the color already established for Prestige/Skill Tree entry points elsewhere in
        /// the game), Orange = leveling/minigame, Dark = the rare/mysterious Meta+Secret
        /// achievements (fits their already-spoiler-heavy tone - see bug #105).</summary>
        private Material CategoryCardMaterial(AchievementCategory cat, bool unlocked)
        {
            switch (cat)
            {
                case AchievementCategory.BookProgress:
                case AchievementCategory.ChapterProgress:
                    return unlocked ? BlueLightMaterial : BlueDarkMaterial;
                case AchievementCategory.ScribeEconomy:
                case AchievementCategory.Managers:
                    return unlocked ? GreenLightMaterial : GreenDarkMaterial;
                case AchievementCategory.Prestige:
                    return unlocked ? PurpleLightMaterial : PurpleDarkMaterial;
                case AchievementCategory.Leveling:
                case AchievementCategory.Minigame:
                    return unlocked ? OrangeLightMaterial : OrangeDarkMaterial;
                case AchievementCategory.Meta:
                case AchievementCategory.Secret:
                    return unlocked ? DarkLightMaterial : DarkDarkMaterial;
                default:
                    return null;
            }
        }

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

        /// <summary>Filter dropdown (2026-08-10, real user redesign ask - "the buttons go off the
        /// screen to the right for every tab... have there be an all button and a filter button,
        /// something that has a drop down with checkboxes in it"). Replaces the old one-tab-per-
        /// category row (which relied on an invisible horizontal-drag ScrollRect the player had no
        /// way to know existed) with a fixed Achieved/Filter button row plus a checkbox dropdown -
        /// multi-select, OR'd together, so several categories can be viewed at once.
        ///
        /// The row's original third button, "All," was removed the same day (real user correction -
        /// it named the exact same word as the unrelated bottom-right scope button, "two different
        /// All's on screen" reading as confusing/redundant). Its only job - clearing category
        /// selection and Achieved-only - didn't need its own button once Achieved became a real
        /// on/off toggle instead of a one-way switch: turning Achieved back off already gets you to
        /// the same "show everything" state.</summary>
        [Header("Filter dropdown (2026-08-10 redesign)")]
        public Button AchievedTabButton;
        public Button FilterButton;
        public GameObject FilterDropdownPanel;
        public Transform FilterDropdownContent;
        public GameObject FilterCheckboxTemplate;

        private readonly List<Card> cards = new List<Card>();
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly HashSet<AchievementCategory> selectedCategories = new HashSet<AchievementCategory>(); // empty = "All"
        private bool unlockedOnlyFilter; // true only while the "Achieved" tab is active
        private string searchText = "";

        /// <summary>Optional external entry point (2026-08-10, kept from the original Phase 4
        /// design) - set this right before loading this scene to land directly in a specific book's
        /// scope instead of the default (active book). Same static-field handoff pattern as
        /// GameLoopController.PendingNewGameStartingBookResourceId, since no AchievementScreenUI
        /// instance exists yet at the moment a caller elsewhere would set this. Consumed once in
        /// Awake() then cleared. Nothing in this project currently sets it - the in-scene book
        /// picker below replaced the need for a scene-reload per book switch - but it's left wired
        /// for any future caller that wants to jump straight into one book's page.</summary>
        public static string PendingBookFilterResourceId;

        /// <summary>The book currently being viewed - ALWAYS has a value (defaults to the active
        /// book in Awake(), never null while this screen is alive), regardless of allMode. Real
        /// scope is bookFilterResourceId + !allMode; switching to allMode doesn't clear this, so
        /// flipping back off All returns to whatever book was last picked, not always the active one.</summary>
        private string bookFilterResourceId;

        /// <summary>Real user redesign (2026-08-10) - true shows gameplay achievements + every book
        /// the player has UNLOCKED (not just the active one); false (the default) scopes to just
        /// bookFilterResourceId. Replaces the old "bookFilterResourceId == null means main screen"
        /// scheme now that scope switches happen in place instead of via scene reload.</summary>
        private bool allMode;

        private readonly List<GalleryCard> galleryCards = new List<GalleryCard>();
        private bool galleryMode;
        private bool galleryShowLocked;

        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            // Default scope (2026-08-10 redesign): whatever book the player is currently reading,
            // not the old "gameplay + active book combined" main view - matches the user's own
            // mental model ("on the main screen is the achievements for the current book"). A
            // pending external override (see PendingBookFilterResourceId's doc comment) still wins
            // if set.
            bookFilterResourceId = !string.IsNullOrEmpty(PendingBookFilterResourceId)
                ? PendingBookFilterResourceId
                : Controller?.ActiveBookResourceId;
            PendingBookFilterResourceId = null;
            allMode = false;

            if (BackButton != null) BackButton.onClick.AddListener(GoBack);
            if (SearchInput != null) SearchInput.onValueChanged.AddListener(OnSearchChanged);

            if (AllScopeButton != null)
            {
                var label = AllScopeButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = "All";
                AllScopeButton.onClick.AddListener(() =>
                {
                    allMode = true;
                    selectedCategories.Clear();
                    unlockedOnlyFilter = false;
                    if (FilterDropdownPanel != null) FilterDropdownPanel.SetActive(false);
                    RefreshScopeUi();
                    ApplyFilter();
                    Refresh();
                });
            }

            BuildBookPicker();
            BuildTabs();
            BuildCards();
            BuildGalleryUi();
            RefreshScopeUi();
            ApplyFilter();
            Refresh();

            if (Controller != null) Controller.OnStateChanged += Refresh;
            if (Controller?.Achievements != null) Controller.Achievements.OnAchievementUnlocked += HandleAchievementUnlocked;
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
            if (Controller?.Achievements != null) Controller.Achievements.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        private void HandleAchievementUnlocked(AchievementDefinition def)
        {
            if (UnlockBurst == null) return;

            var card = cards.Find(c => c.Def.id == def.id);
            if (card != null && card.Root != null && card.Root.activeInHierarchy)
            {
                var cardRt = card.Root.GetComponent<RectTransform>();
                UnlockBurst.PlayAt(cardRt, Vector2.zero);
            }
            else if (BurstFallbackAnchor != null)
            {
                UnlockBurst.PlayAt(BurstFallbackAnchor, Vector2.zero);
            }
        }

        /// <summary>Real user correction (2026-08-10, extended same day for bug #104): Back is a
        /// real breadcrumb now, one step at a time, matching how the player actually got here -
        /// Gallery steps back to the card list (SetGalleryMode(false), no scene change); All-scope
        /// steps back to whichever book's page the player was on before clicking All (allMode=false,
        /// still no scene change - bug #104's real complaint was that this step was skipped
        /// entirely, jumping straight to the game from the aggregate view); only Book-scope's own
        /// Back actually leaves the Achievements screen, since that's the sole way back to gameplay
        /// from anywhere in this flow. Label text for each state lives in RefreshScopeUi()/
        /// SetGalleryMode() so it can never drift out of sync with what the button will actually do.</summary>
        private void GoBack()
        {
            if (galleryMode) { SetGalleryMode(false); return; }
            if (allMode)
            {
                allMode = false;
                RefreshScopeUi();
                ApplyFilter();
                Refresh();
                return;
            }
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

        /// <summary>Real user redesign (2026-08-10): the old one-tab-per-category row overflowed
        /// off-screen with no visible way to reach the hidden tabs (it technically scrolled via
        /// drag, but nothing signaled that). Replaced with a fixed All/Achieved/Filter row - Filter
        /// opens a checkbox dropdown (multi-select, OR'd together) built once from whichever
        /// categories actually have achievements, same "don't build a dead row for empty
        /// categories" rule the old tab-builder used.</summary>
        private void BuildTabs()
        {
            if (Controller?.Achievements == null) return;

            if (AchievedTabButton != null)
                AchievedTabButton.onClick.AddListener(() =>
                {
                    unlockedOnlyFilter = !unlockedOnlyFilter;
                    if (FilterDropdownPanel != null) FilterDropdownPanel.SetActive(false);
                    ApplyFilter();
                });

            if (FilterButton != null)
                FilterButton.onClick.AddListener(() =>
                {
                    if (FilterDropdownPanel == null) return;
                    bool willOpen = !FilterDropdownPanel.activeSelf;
                    if (willOpen) PositionDropdown(FilterButton, FilterDropdownPanel);
                    FilterDropdownPanel.SetActive(willOpen);
                });

            BuildFilterDropdown();
            ThemeFilterRow();

            // Draw on top of the card grid regardless of scene sibling order.
            if (FilterDropdownPanel != null) FilterDropdownPanel.transform.SetAsLastSibling();
        }

        /// <summary>Real user correction (2026-08-10): the dropdown's anchored position was a
        /// leftover fixed value from before the tab row got centered, so it opened floating over
        /// the "All" button instead of under "Filter". Buttons live several layout groups deep
        /// (TabScrollView/.../TabContainer/FilterButton), so a fixed local offset can't track it -
        /// convert the button's actual world-space bottom-center into the panel's parent's local
        /// space instead, which stays correct regardless of how the row above gets laid out.
        ///
        /// Second real user correction (same day): even with the math fixed, a left-aligned panel
        /// with an 8px gap still read as "a separate floating card", not something that visibly
        /// belongs to the Filter button. Now centers under the button (panelRt pivot is (0.5,1) to
        /// match), sits flush against it (no gap).
        ///
        /// Third real correction (still 2026-08-10): this originally only opened DOWNWARD, which
        /// broke the moment the user relocated BookPickerButton to the bottom of the screen (no room
        /// below - panel rendered almost entirely off-canvas). A hardcoded "always open above"
        /// fallback was added for that case, which then broke AGAIN the moment the user relocated
        /// the SAME button to the TOP of the screen while rearranging the layout themselves ("it
        /// loads up instead of down so you can't read it"). Any button on this screen can end up
        /// anywhere the user drags it, so the direction can no longer be hardcoded per-button -
        /// this now measures actual on-screen room above vs. below the anchor button (in real
        /// Screen-space pixels, not canvas-local units, since that's what's actually finite) and
        /// opens whichever side has more space, setting the panel's pivot to match (top-center to
        /// open downward, bottom-center to open upward) so it always renders fully visible
        /// regardless of where the button currently lives. Serves both FilterButton/FilterDropdownPanel
        /// and BookPickerButton/BookPickerPanel - same positioning need, just different pairs.</summary>
        private void PositionDropdown(Button anchorButton, GameObject panel)
        {
            if (anchorButton == null || panel == null) return;
            var btnRt = anchorButton.GetComponent<RectTransform>();
            var panelRt = panel.GetComponent<RectTransform>();
            var parentRt = panelRt.parent as RectTransform;
            if (btnRt == null || panelRt == null || parentRt == null) return;

            var corners = new Vector3[4];
            btnRt.GetWorldCorners(corners); // 0=bl 1=tl 2=tr 3=br

            var canvas = parentRt.GetComponentInParent<Canvas>();
            var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            float btnBottomScreenY = RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y;
            float btnTopScreenY = RectTransformUtility.WorldToScreenPoint(cam, corners[1]).y;
            float roomBelow = btnBottomScreenY; // screen space: y=0 is the bottom of the window
            float roomAbove = Screen.height - btnTopScreenY;
            bool openBelow = roomBelow >= roomAbove;

            panelRt.pivot = new Vector2(0.5f, openBelow ? 1f : 0f);
            Vector3 anchorPoint = openBelow ? (corners[0] + corners[3]) * 0.5f : (corners[1] + corners[2]) * 0.5f;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, anchorPoint);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screenPoint, cam, out var localPoint))
            {
                // localPoint is relative to parentRt's own pivot, so re-base it onto the parent's
                // top-left corner to get a valid anchoredPosition for whatever the panel's pivot is.
                float parentTopLeftX = -parentRt.pivot.x * parentRt.rect.width;
                float parentTopLeftY = (1f - parentRt.pivot.y) * parentRt.rect.height;
                panelRt.anchoredPosition = new Vector2(localPoint.x - parentTopLeftX, localPoint.y - parentTopLeftY);
            }
        }

        private class BookPickerRow
        {
            public string ResourceId;
            public Button Button;
            public TMP_Text Reference;
            public TMP_Text Cost;
            public HoverTooltip Tooltip;
        }

        private readonly List<BookPickerRow> bookPickerRows = new List<BookPickerRow>();

        /// <summary>Real user redesign (2026-08-10) - "there needs to be a drop down to select which
        /// book they wanna look at... switch between the different books here to see which
        /// achievements they have." Same Books-tab convention as BookListUI (list every OT book,
        /// gray/disable the ones not yet unlocked) rather than hiding locked books outright - the
        /// user's explicit call after weighing both ("book existence isn't a spoiler here, only
        /// achievement content is"). Rows are built once (CanonicalBookOrder never changes at
        /// runtime); Refresh only updates existing text/interactable state.</summary>
        private void BuildBookPicker()
        {
            if (BookPickerButton != null)
                BookPickerButton.onClick.AddListener(() =>
                {
                    if (BookPickerPanel == null) return;
                    bool willOpen = !BookPickerPanel.activeSelf;
                    if (willOpen) { PositionDropdown(BookPickerButton, BookPickerPanel); RefreshBookPickerRows(); }
                    BookPickerPanel.SetActive(willOpen);
                });

            if (BookPickerContent == null || BookPickerRowTemplate == null || Controller == null) return;

            foreach (var (resourceId, displayName) in Controller.AllBooksInOrder)
            {
                var rowGo = Instantiate(BookPickerRowTemplate, BookPickerContent);
                rowGo.SetActive(true);
                rowGo.name = "BookPickerRow_" + resourceId;

                var reference = rowGo.transform.Find("Reference")?.GetComponent<TMP_Text>();
                var cost = rowGo.transform.Find("Cost")?.GetComponent<TMP_Text>();
                if (reference != null) reference.text = displayName;

                string id = resourceId;
                var button = rowGo.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(() =>
                    {
                        if (Controller == null || !Controller.IsBookUnlocked(id)) return;
                        bookFilterResourceId = id;
                        allMode = false;
                        selectedCategories.Clear();
                        unlockedOnlyFilter = false;
                        if (FilterDropdownPanel != null) FilterDropdownPanel.SetActive(false);
                        if (BookPickerPanel != null) BookPickerPanel.SetActive(false);
                        RefreshScopeUi();
                        ApplyFilter();
                        Refresh();
                    });

                bookPickerRows.Add(new BookPickerRow
                {
                    ResourceId = id,
                    Button = button,
                    Reference = reference,
                    Cost = cost,
                    Tooltip = rowGo.gameObject.AddComponent<HoverTooltip>()
                });
            }

            if (BookPickerPanel != null) BookPickerPanel.transform.SetAsLastSibling();
        }

        private static readonly Color BookPickerUnlockedColor = new Color(0.12f, 0.09f, 0.05f, 1f);
        // Bug #102 (2026-08-10): the original locked color (0.55,0.5,0.45) sat too close in
        // luminance to the panel's own tan (0.86,0.78,0.6) background, reading as "fading into the
        // background" - darkened while keeping it clearly lighter/more muted than the unlocked
        // near-black, so the distinction between the two states stays legible either way.
        private static readonly Color BookPickerLockedColor = new Color(0.4f, 0.35f, 0.27f, 1f);

        private void RefreshBookPickerRows()
        {
            if (Controller == null) return;
            foreach (var row in bookPickerRows)
            {
                bool unlocked = Controller.IsBookUnlocked(row.ResourceId);
                bool viewing = !allMode && row.ResourceId == bookFilterResourceId;

                if (row.Button != null) row.Button.interactable = unlocked;
                if (row.Reference != null) row.Reference.color = unlocked ? BookPickerUnlockedColor : BookPickerLockedColor;
                if (row.Cost != null) row.Cost.text = !unlocked ? "Locked" : viewing ? "Viewing" : "View";
                if (row.Tooltip != null)
                {
                    row.Tooltip.enabled = !unlocked;
                    row.Tooltip.Text = "Can be unlocked in the Skill Tree after a prestige is done.";
                }
            }
        }

        /// <summary>Updates every scope-dependent control after allMode or bookFilterResourceId
        /// changes (2026-08-10 redesign): AllScopeButton hides once already in All (nothing left for
        /// it to do), FilterButton/its dropdown only make sense while in All (a single book's
        /// achievements are already one small set - filtering by category doesn't add anything, per
        /// the user's explicit call), and BookPickerButton ALSO hides in All (real correction,
        /// same day - "that button shouldn't even be visible... you're not looking at books, you're
        /// looking at all of the achievements" - so a book-scope-only control has no place there).
        /// Also closes BookPickerPanel when it's about to be hidden, so it can never be left open
        /// and unreachable behind a hidden button.
        ///
        /// Bug #104 (same day): also keeps the screen title and Back button label honest about
        /// current scope - title reads "ALL ACHIEVEMENTS" in All-scope so it's visually unambiguous
        /// which view is active (previously stuck on "ACHIEVEMENTS" always), and Back reads "Back to
        /// {Book}" in All-scope since that's genuinely where it goes now (see GoBack). Skipped
        /// entirely while galleryMode is active - SetGalleryMode owns both of those exclusively
        /// while the gallery is open, and this method isn't expected to run during that state
        /// anyway (the buttons it touches are hidden behind the gallery view).</summary>
        private void RefreshScopeUi()
        {
            if (AllScopeButton != null) AllScopeButton.gameObject.SetActive(!allMode);

            if (FilterButton != null) FilterButton.gameObject.SetActive(allMode);
            if (!allMode && FilterDropdownPanel != null) FilterDropdownPanel.SetActive(false);

            if (BookPickerButton != null) BookPickerButton.gameObject.SetActive(!allMode);
            if (allMode && BookPickerPanel != null) BookPickerPanel.SetActive(false);

            if (!allMode && BookPickerButton != null)
            {
                var label = BookPickerButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = CanonicalBookOrder.DisplayNameOf(bookFilterResourceId) + " ▾";
            }

            if (!galleryMode)
            {
                if (ScreenTitle != null) ScreenTitle.text = allMode ? "ALL ACHIEVEMENTS" : "ACHIEVEMENTS";
                if (BackButton != null)
                {
                    var backLabel = BackButton.GetComponentInChildren<TMP_Text>();
                    if (backLabel != null)
                        backLabel.text = allMode
                            ? $"Back to {CanonicalBookOrder.DisplayNameOf(bookFilterResourceId)}"
                            : "Back to Game";
                }
            }

            RefreshBookPickerRows();
        }

        /// <summary>Real user correction (2026-08-10): the row previously used a flat MetallicUI tab
        /// skin that didn't match the rest of the app, and sat left-anchored instead of centered.
        /// Re-skins both remaining buttons with the exact same wooden sprite the Back button uses
        /// (so they "fit the theme of the entire application") - Filter is a lighter tan ("since it
        /// has to have filters selected to be useful"), Achieved's color is now dynamic (see
        /// RefreshAchievedButtonVisual - neutral tan matching everything else when off, green when
        /// actively toggled on) - and centers the row horizontally instead of left-anchoring it.</summary>
        private void ThemeFilterRow()
        {
            var backImg = BackButton != null ? BackButton.GetComponent<Image>() : null;
            var wooden = backImg != null ? backImg.sprite : null;
            var filterColor = new Color(0.95f, 0.9f, 0.74f, 1f); // lighter tan

            SkinFilterRowButton(FilterButton, wooden, filterColor);
            SkinFilterRowButton(AchievedTabButton, wooden, AchievedInactiveColor);
            RefreshAchievedButtonVisual();

            if (TabContainer is RectTransform tabRt)
            {
                var anchorMin = tabRt.anchorMin;
                var anchorMax = tabRt.anchorMax;
                var pivot = tabRt.pivot;
                var pos = tabRt.anchoredPosition;
                anchorMin.x = 0.5f;
                anchorMax.x = 0.5f;
                pivot.x = 0.5f;
                pos.x = 0f;
                tabRt.anchorMin = anchorMin;
                tabRt.anchorMax = anchorMax;
                tabRt.pivot = pivot;
                tabRt.anchoredPosition = pos;
            }
        }

        private static void SkinFilterRowButton(Button btn, Sprite sprite, Color tint)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            if (sprite != null) img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = tint;
        }

        /// <summary>One checkbox row per category that actually has at least one achievement.
        /// Reuses the same small-square checkbox visual language as the Settings screen redesign
        /// (2026-08-09) instead of introducing a new control style.</summary>
        private void BuildFilterDropdown()
        {
            if (FilterDropdownContent == null || FilterCheckboxTemplate == null) return;

            var categoriesPresent = Controller.Achievements.AllDefinitions
                .Select(d => d.category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var cat in categoriesPresent)
            {
                var rowGo = Instantiate(FilterCheckboxTemplate, FilterDropdownContent);
                rowGo.SetActive(true);
                rowGo.name = "FilterRow_" + cat;

                var label = rowGo.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null) label.text = SplitCategoryName(cat);

                var checkbox = rowGo.transform.Find("Checkbox")?.GetComponent<Button>();
                var checkboxImg = checkbox?.GetComponent<Image>();
                if (checkbox != null)
                {
                    checkbox.onClick.AddListener(() =>
                    {
                        if (!selectedCategories.Remove(cat)) selectedCategories.Add(cat);
                        unlockedOnlyFilter = false;
                        RefreshFilterCheckboxVisuals();
                        RefreshFilterButtonLabel();
                        ApplyFilter();
                    });
                }
                filterCheckboxes.Add((cat, checkboxImg));
            }
            RefreshFilterCheckboxVisuals();
        }

        private readonly List<(AchievementCategory cat, Image checkboxImg)> filterCheckboxes = new List<(AchievementCategory, Image)>();
        private static readonly Color CheckboxOnColor = new Color(0.35f, 0.55f, 0.3f, 1f);
        private static readonly Color CheckboxOffColor = new Color(0.3f, 0.27f, 0.2f, 1f);

        private void RefreshFilterCheckboxVisuals()
        {
            foreach (var (cat, img) in filterCheckboxes)
                if (img != null) img.color = selectedCategories.Contains(cat) ? CheckboxOnColor : CheckboxOffColor;
        }

        private void RefreshFilterButtonLabel()
        {
            if (FilterButton == null) return;
            var text = FilterButton.GetComponentInChildren<TMP_Text>();
            if (text == null) return;
            text.text = selectedCategories.Count == 0 ? "Filter ▾" : $"Filter ({selectedCategories.Count}) ▾";
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
                    var iconSize = slotSize * 0.4f;
                    iconRt.sizeDelta = iconSize;
                    iconRt.anchoredPosition -= (iconSize - slotSize) * (new Vector2(0.5f, 0.5f) - iconRt.pivot);
                }

                // Card-sized shimmer border (2026-08-12) - drawn as the LAST sibling so it sits on
                // top of every other element, but its own art is hollow in the middle (real border
                // data, not a full fill), so it only ever contributes a framing ring around the
                // card's edge - text underneath stays fully legible.
                Image cardBorder = null;
                if (CardBorderSprite != null)
                {
                    var cardRt = cardGo.GetComponent<RectTransform>();
                    var borderGo = new GameObject("CardBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    borderGo.transform.SetParent(cardGo.transform, false);
                    var borderRt = borderGo.GetComponent<RectTransform>();
                    borderRt.anchorMin = Vector2.zero;
                    borderRt.anchorMax = Vector2.one;
                    borderRt.offsetMin = Vector2.zero;
                    borderRt.offsetMax = Vector2.zero;
                    cardBorder = borderGo.GetComponent<Image>();
                    cardBorder.sprite = CardBorderSprite;
                    cardBorder.type = Image.Type.Sliced;
                    cardBorder.raycastTarget = false;
                    // Default multiplier renders this border thinly enough to nearly disappear at
                    // card scale - thickened so the ornate corners and shimmer actually read.
                    cardBorder.pixelsPerUnitMultiplier = 0.45f;
                    borderGo.transform.SetAsLastSibling();
                }

                var card = new Card
                {
                    Def = def,
                    Root = cardGo,
                    CardBackground = cardGo.GetComponent<Image>(),
                    Icon = icon,
                    Frame = frame,
                    CardBorder = cardBorder,
                    NameText = cardGo.transform.Find("Name")?.GetComponent<TMP_Text>(),
                    DescText = cardGo.transform.Find("Description")?.GetComponent<TMP_Text>(),
                    ProgressLabel = cardGo.transform.Find("ProgressBar/ProgressLabel")?.GetComponent<TMP_Text>(),
                    ProgressFill = cardGo.transform.Find("ProgressBar/Fill")?.GetComponent<Image>(),
                    Tooltip = cardGo.GetComponent<HoverTooltip>()
                };
                cards.Add(card);
            }
        }

        /// <summary>Wires the gallery mode's open/back/show-locked buttons and builds the dense
        /// icon-only tile grid (Phase 5, 2026-08-10). No-ops gracefully if the gallery UI wasn't
        /// wired into this scene (older AchievementScreen.unity saves, or the AchievementScreen
        /// used purely for a book-mode navigation reload where gallery fields might be null).</summary>
        private void BuildGalleryUi()
        {
            if (GalleryOpenButton != null)
                GalleryOpenButton.onClick.AddListener(() => SetGalleryMode(true));
            if (GalleryShowLockedToggle != null)
                GalleryShowLockedToggle.onClick.AddListener(() =>
                {
                    galleryShowLocked = !galleryShowLocked;
                    RefreshGalleryShowLockedLabel();
                    ApplyGalleryFilter();
                });
            RefreshGalleryShowLockedLabel();

            BuildGalleryCards();
            SetGalleryMode(false);
        }

        private void RefreshGalleryShowLockedLabel()
        {
            if (GalleryShowLockedLabel != null)
                GalleryShowLockedLabel.text = galleryShowLocked ? "Hide Locked" : "Show Locked";
        }

        /// <summary>Switches between the normal descriptive card grid and the dense trophy gallery -
        /// both live in the same scene/screen (not a separate scene), same reuse pattern as book
        /// mode. Resets the shared description panel to its idle state on entry so it doesn't show
        /// stale content from before the mode switch. Also updates the single shared BackButton's
        /// label to match what it will actually do (see GoBack's doc comment).</summary>
        private void SetGalleryMode(bool enabled)
        {
            galleryMode = enabled;
            if (NormalScrollView != null) NormalScrollView.SetActive(!enabled);
            if (GalleryScrollView != null) GalleryScrollView.SetActive(enabled);
            if (GalleryOpenButton != null) GalleryOpenButton.gameObject.SetActive(!enabled);

            if (BackButton != null)
            {
                var label = BackButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    // Entering gallery always says "Back to List" - leaving it hands the label back
                    // to RefreshScopeUi() instead of hardcoding "Back to Game", since bug #104 means
                    // leaving the gallery can land back in All-scope too ("Back to {Book}", not the
                    // game) depending on what was active before the gallery was opened.
                    if (enabled) label.text = "Back to List";
                }
            }
            if (!enabled) RefreshScopeUi();

            ClearGalleryDescription();
            if (enabled) ApplyGalleryFilter();
        }

        private void ClearGalleryDescription()
        {
            if (GalleryDescriptionName != null) GalleryDescriptionName.text = "Hover a trophy to see what it is.";
            if (GalleryDescriptionBody != null) GalleryDescriptionBody.text = "";
        }

        /// <summary>Builds one small icon+frame tile per achievement definition, same diamond
        /// shape/frame-sprite technique as the main card grid's icon (reused, not reinvented), just
        /// without any Name/Description/ProgressBar children - those live in the single shared
        /// description panel instead (2026-08-10, explicit user ask to avoid "literally... a hover
        /// text little panel" per tile at this density).</summary>
        private void BuildGalleryCards()
        {
            if (Controller?.Achievements == null || GalleryContent == null || GalleryCardTemplate == null) return;

            foreach (var def in OrderedDefinitions())
            {
                var tileGo = Instantiate(GalleryCardTemplate, GalleryContent);
                tileGo.SetActive(true);
                tileGo.name = "GalleryTile_" + def.id;

                var icon = tileGo.transform.Find("Icon")?.GetComponent<Image>();
                Image frame = null;
                if (icon != null)
                {
                    icon.sprite = NodeShapeSprites.Get(Progression.SkillNodeShape.Diamond);
                    icon.type = Image.Type.Simple;

                    var iconRt = icon.GetComponent<RectTransform>();
                    var slotSize = iconRt.sizeDelta;

                    var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    frameGo.transform.SetParent(icon.transform.parent, false);
                    var frameRt = frameGo.GetComponent<RectTransform>();
                    frameRt.anchorMin = iconRt.anchorMin;
                    frameRt.anchorMax = iconRt.anchorMax;
                    frameRt.pivot = iconRt.pivot;
                    var frameSize = slotSize + new Vector2(14f, 14f);
                    frameRt.sizeDelta = frameSize;
                    frameRt.anchoredPosition = iconRt.anchoredPosition - (frameSize - slotSize) * (new Vector2(0.5f, 0.5f) - iconRt.pivot);
                    frame = frameGo.GetComponent<Image>();
                    frame.sprite = FrameFor(def.tier);
                    frame.type = Image.Type.Simple;
                    frame.raycastTarget = false;
                    frameGo.transform.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);

                    var iconSize = slotSize * 0.4f;
                    iconRt.sizeDelta = iconSize;
                    iconRt.anchoredPosition -= (iconSize - slotSize) * (new Vector2(0.5f, 0.5f) - iconRt.pivot);
                }

                // Bug #126 (2026-08-16): hovering a diamond correctly updated the description text
                // above, but nothing on the diamond itself showed which one was hovered - added a
                // real Outline glow (same technique as the Skill Tree's owned-node glow and the
                // Settings toggle-button glow), off by default, toggled by the same hover handlers
                // that already drive the description text.
                var glow = (frame != null ? frame.gameObject : icon.gameObject).AddComponent<Outline>();
                glow.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
                glow.effectDistance = new Vector2(4f, -4f);
                glow.enabled = false;

                var galleryCard = new GalleryCard { Def = def, Root = tileGo, Icon = icon, Frame = frame, HoverGlow = glow };
                galleryCards.Add(galleryCard);

                // Shared-panel hover (not a per-tile tooltip) via a runtime EventTrigger - avoids
                // needing a whole new MonoBehaviour file just to relay two pointer events back to
                // this screen's own shared description fields.
                var trigger = tileGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener(_ =>
                {
                    ShowGalleryDescription(galleryCard.Def);
                    if (galleryCard.HoverGlow != null) galleryCard.HoverGlow.enabled = true;
                });
                trigger.triggers.Add(enterEntry);
                var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exitEntry.callback.AddListener(_ =>
                {
                    ClearGalleryDescription();
                    if (galleryCard.HoverGlow != null) galleryCard.HoverGlow.enabled = false;
                });
                trigger.triggers.Add(exitEntry);
            }
        }

        private void ShowGalleryDescription(AchievementDefinition def)
        {
            if (Controller?.Achievements == null) return;
            bool unlocked = Controller.Achievements.IsUnlocked(def.id);
            bool reveal = unlocked || !def.spoiler;

            if (GalleryDescriptionName != null) GalleryDescriptionName.text = reveal ? def.displayName : HiddenAchievementName;
            if (GalleryDescriptionBody != null)
            {
                string desc = reveal ? def.description : HiddenAchievementDesc;
                if (unlocked)
                {
                    string rawTs = Controller.Achievements.GetUnlockedAtUtc(def.id);
                    string when = "unknown date";
                    if (!string.IsNullOrEmpty(rawTs) && DateTime.TryParse(rawTs, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                        when = parsed.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt");
                    desc += $"\n\nEarned: {when}";
                }
                GalleryDescriptionBody.text = desc;
            }
        }

        /// <summary>Shows every EARNED achievement always (a real trophy case - already-known
        /// content can't be spoiled by showing it, regardless of which book it came from or
        /// whether that book is still active). With Show Locked on, also shows LOCKED tiles as
        /// silhouettes, but ONLY for achievements already in the main screen's own staged scope
        /// (current book + revealed gameplay tiers) - never a locked tile for a book not yet
        /// reached or a still-hidden late-game gameplay tier, so the gallery can never be used to
        /// peek at scope the player hasn't earned the right to see (2026-08-10, explicit user
        /// decision after considering "both" earned-and-full-list views).</summary>
        private void ApplyGalleryFilter()
        {
            if (Controller?.Achievements == null) return;
            foreach (var tile in galleryCards)
            {
                bool unlocked = Controller.Achievements.IsUnlocked(tile.Def.id);
                bool showAsLocked = !unlocked && galleryShowLocked && IsInStagedScope(tile.Def) && !tile.Def.spoiler;
                tile.Root.SetActive(unlocked || showAsLocked);
            }
        }

        /// <summary>Staged visibility (2026-08-10, Phase 3 of the v2 redesign; reworked again same
        /// day for the in-place scope-switching redesign - book scope is now the DEFAULT, not
        /// something navigated into). Two modes, driven by allMode instead of a null check:
        /// - Book scope (allMode false, the default): ONLY bookFilterResourceId's achievements.
        ///   Stays live/earnable even after the player has moved past that book (2026-08-10 resolved
        ///   design question - managers/submanagers there remain purchasable). Gameplay achievements
        ///   are excluded here - a book's page is its own set, not mixed with the grand total.
        /// - All scope (allMode true): gameplay/economy achievements (bookResourceId empty -
        ///   section-grouping/ultimate achievements included, since their own spoiler flag already
        ///   hides their content the same way) + every book the player has UNLOCKED (not just
        ///   active) - "all of the achievements that they can actually obtain," not literally every
        ///   book in the Bible regardless of progress. A book not yet unlocked is excluded entirely
        ///   here too - not spoiler-blanked, literally not a card.
        /// Matches the same progressive-disclosure discipline as the Skill Tree's node visibility
        /// and the Books tab either way.</summary>
        private bool IsInStagedScope(AchievementDefinition def)
        {
            if (Controller?.Achievements == null) return false;

            if (!allMode)
                return def.bookResourceId == bookFilterResourceId;

            return string.IsNullOrEmpty(def.bookResourceId) || Controller.IsBookUnlocked(def.bookResourceId);
        }

        /// <summary>Shows/hides cards per staged visibility + the active category tab + search
        /// text. Search matches against the DISPLAYED text (reveal-aware) - a spoiler-locked
        /// achievement is searchable only by its "???" placeholder, never its real hidden name, so
        /// search can't be used to leak spoilers.</summary>
        private void ApplyFilter()
        {
            if (Controller?.Achievements == null) return;

            foreach (var card in cards)
            {
                if (!IsInStagedScope(card.Def)) { card.Root.SetActive(false); continue; }

                bool unlocked = Controller.Achievements.IsUnlocked(card.Def.id);
                bool categoryMatch = unlockedOnlyFilter
                    ? unlocked
                    : (selectedCategories.Count == 0 || selectedCategories.Contains(card.Def.category));

                bool reveal = unlocked || !card.Def.spoiler;
                string visibleName = reveal ? card.Def.displayName : HiddenAchievementName;
                string visibleDesc = reveal ? card.Def.description : HiddenAchievementDesc;

                bool searchMatch = string.IsNullOrEmpty(searchText) ||
                    visibleName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    visibleDesc.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                card.Root.SetActive(categoryMatch && searchMatch);
            }

            // Centralized here rather than duplicated at every call site that can change
            // unlockedOnlyFilter (Achieved's own click, a category checkbox, AllScopeButton, a book
            // picker row) - ApplyFilter() already runs after all of them, so the button's visual
            // state can never go stale no matter which path changed the underlying value.
            RefreshAchievedButtonVisual();
        }

        private static readonly Color AchievedActiveColor = new Color(0.42f, 0.62f, 0.34f, 1f); // green - "on"
        private static readonly Color AchievedInactiveColor = new Color(0.85f, 0.75f, 0.55f, 1f); // neutral tan - "off", matches every other button's resting color

        /// <summary>Real user redesign (2026-08-10): Achieved is now a real on/off toggle (was a
        /// one-way switch that only "All" could undo) - this keeps its color/label honest about
        /// which state it's actually in. Neutral tan (matching every other button's resting color)
        /// when off, green when on - a real toggle signal instead of a permanently-green button that
        /// gave no visual cue either way.</summary>
        private void RefreshAchievedButtonVisual()
        {
            if (AchievedTabButton == null) return;
            var img = AchievedTabButton.GetComponent<Image>();
            if (img != null) img.color = unlockedOnlyFilter ? AchievedActiveColor : AchievedInactiveColor;
            var text = AchievedTabButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = unlockedOnlyFilter ? "Achieved ✓" : "Achieved";
        }

        private void Refresh()
        {
            if (Controller?.Achievements == null) return;

            // Header shows only the VISIBLE total (2026-08-10, explicit user decision) - never the
            // true global count. In ALL SCOPE, spoiler-hidden achievements in scope aren't counted
            // until earned, at which point the total itself visibly grows. In BOOK SCOPE (now the
            // default), hidden achievements ARE included in the total from the start (explicit user
            // decision - "include the hidden ones for the books... they know there's hidden
            // achievements here") - only their name/description stay hidden, the count itself
            // doesn't.
            if (HeaderText != null)
            {
                int visibleTotal = 0, visibleAchieved = 0;
                foreach (var card in cards)
                {
                    if (!IsInStagedScope(card.Def)) continue;
                    bool unlocked = Controller.Achievements.IsUnlocked(card.Def.id);
                    if (allMode && card.Def.spoiler && !unlocked) continue;
                    visibleTotal++;
                    if (unlocked) visibleAchieved++;
                }
                string countText = visibleTotal > 0
                    ? $"{visibleAchieved} / {visibleTotal} ({(100f * visibleAchieved / visibleTotal):F0}%)"
                    : "0 / 0 (0%)";
                HeaderText.text = !allMode
                    ? $"{CanonicalBookOrder.DisplayNameOf(bookFilterResourceId)} — {countText}"
                    : countText;
            }

            // Re-scope the visible card set every refresh, not just on tab/search changes - a book
            // switch fires OnStateChanged (which calls Refresh), and that's exactly when the staged
            // scope needs to change too.
            ApplyFilter();

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
                    // A spoiler-hidden Progress achievement's numeric count would leak scope info
                    // (e.g. "3/5" hints at the goal) - fall back to "Locked" like the non-Progress
                    // branch below, not HiddenAchievementDesc (that sentence belongs in the
                    // Description box, not this narrow pill - it doesn't fit and reads as broken).
                    if (card.ProgressLabel != null)
                        card.ProgressLabel.text = reveal ? $"{progress:N0} / {goal:N0}" : "Locked";
                }
                else
                {
                    SetFill(card.ProgressFill, unlocked ? 1f : 0f);
                    if (card.ProgressLabel != null) card.ProgressLabel.text = unlocked ? "Unlocked" : "Locked";
                }

                // Real per-category icon art where available (2026-08-09), else the procedural
                // diamond shape tinted by category - but ONLY once earned (2026-08-10 correction).
                // A locked card shows the lock icon regardless of category, same rule the Trophy
                // Gallery already enforced for its own tiles - no real icon art leaks a category
                // hint before the achievement is actually unlocked.
                if (card.Icon != null)
                {
                    if (!unlocked && LockedIcon != null)
                    {
                        card.Icon.sprite = LockedIcon;
                        card.Icon.color = Color.white;
                    }
                    else if (!unlocked)
                    {
                        card.Icon.sprite = NodeShapeSprites.Get(Progression.SkillNodeShape.Diamond);
                        card.Icon.color = LockedIconFallbackColor;
                    }
                    else
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

                // Shader-driven shimmer (2026-08-10, extended 2026-08-12 to the actual card panel
                // border per user correction - "the background frames, not the metallic frames
                // around the icons"). Same category+lock-state material on both: the small tier-rank
                // diamond AND the full card's own border ring.
                var cardMat = CategoryCardMaterial(def.category, unlocked);
                if (cardMat != null)
                {
                    if (card.Frame != null) card.Frame.material = cardMat;
                    if (card.CardBorder != null) card.CardBorder.material = cardMat;
                }
            }

            RefreshGallery();
        }

        private static readonly Color GalleryEarnedColor = Color.white;
        private static readonly Color GalleryLockedSilhouetteColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        private static readonly Color LockedIconFallbackColor = new Color(0.45f, 0.32f, 0.18f, 1f);

        /// <summary>Updates gallery tile visuals (earned = full color, locked = the same lock icon
        /// the main card grid uses, dark-tinted to stay a quiet silhouette at this density rather
        /// than the earned tiles' full color - no per-category art showing through either way, a
        /// locked achievement's identity shouldn't be guessable from its icon) and re-scopes which
        /// tiles are active. No-ops if the gallery UI isn't wired (older scene saves).</summary>
        private void RefreshGallery()
        {
            if (Controller?.Achievements == null || galleryCards.Count == 0) return;

            ApplyGalleryFilter();

            foreach (var tile in galleryCards)
            {
                bool unlocked = Controller.Achievements.IsUnlocked(tile.Def.id);
                if (tile.Icon != null)
                {
                    if (unlocked)
                    {
                        var realIcon = CategoryIconSprite(tile.Def.category);
                        if (realIcon != null) { tile.Icon.sprite = realIcon; tile.Icon.color = GalleryEarnedColor; }
                        else { tile.Icon.sprite = NodeShapeSprites.Get(Progression.SkillNodeShape.Diamond); tile.Icon.color = CategoryFallbackColor(tile.Def.category); }
                    }
                    else if (LockedIcon != null)
                    {
                        tile.Icon.sprite = LockedIcon;
                        tile.Icon.color = GalleryLockedSilhouetteColor;
                    }
                    else
                    {
                        tile.Icon.sprite = NodeShapeSprites.Get(Progression.SkillNodeShape.Diamond);
                        tile.Icon.color = GalleryLockedSilhouetteColor;
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
