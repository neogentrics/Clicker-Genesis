using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// The Buy Verse screen: spend Ink to reveal the next verse, and review any previously
    /// unlocked verse via the list built by VerseListUI. Tapping lives on ClickerScreen.
    /// </summary>
    public class BuyVerseScreenUI : MonoBehaviour
    {
        public TMP_Text InkLabel;
        public TMP_Text ReferenceLabel;
        public TMP_Text VerseText;
        public TMP_Text StatusLabel;
        public Button BuyButton;
        public TMP_Text BuyButtonLabel;
        public ScrollRect VerseScrollRect;

        [Header("Bulk buy (1x/2x/3x/4x = 1/5/10/20 verses)")]
        public Button MultiplierButton;
        public TMP_Text MultiplierButtonLabel;

        [Header("Triple-lane bar (mirrors ClickerScreenUI - book/XP/Old Testament progress)")]
        public Image BookLaneFill;
        public TMP_Text BookLaneLabel;
        public Image XpLaneFill;
        public TMP_Text XpLaneLabel;
        public Image OtLaneFill;
        public TMP_Text OtLaneLabel;

        [Header("Verses/Chapters/Books tabs")]
        public Button VersesTabButton;
        public Button ChaptersTabButton;
        public Button BooksTabButton;
        public GameObject VerseListRoot;
        public GameObject ChapterListRoot;
        public GameObject BookListRoot;
        public Image VersesTabBackground;
        public Image ChaptersTabBackground;
        public Image BooksTabBackground;
        public VerseListUI VerseList;

        private enum ScreenTab { Verses, Chapters, Books }

        /// <summary>-1 = nothing selected yet (defaults to the most recently unlocked verse).</summary>
        private int selectedVerseIndex = -1;
        private ScreenTab currentTab = ScreenTab.Verses;
        private bool showingChapters => currentTab == ScreenTab.Chapters;

        /// <summary>Tracks the verse actually displayed in the reading box, distinct from
        /// selectedVerseIndex - lets Refresh() (called every frame by passive Ink ticking) skip
        /// the expensive text-rebuild/force-refresh work except on the frame it truly changes.</summary>
        private int lastDisplayedIndex = int.MinValue;

        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            // See ClickerScreenUI.Awake() for why this wiring lives here, not in Start().
            BuyButton.onClick.AddListener(HandleBuy);
            if (MultiplierButton != null) MultiplierButton.onClick.AddListener(HandleMultiplierOrUnlockClick);
            if (VersesTabButton != null) VersesTabButton.onClick.AddListener(() => SetTab(ScreenTab.Verses));
            if (ChaptersTabButton != null) ChaptersTabButton.onClick.AddListener(() => SetTab(ScreenTab.Chapters));
            if (BooksTabButton != null) BooksTabButton.onClick.AddListener(() => SetTab(ScreenTab.Books));
            if (Controller != null) Controller.OnStateChanged += Refresh;
            SetTab(ScreenTab.Verses);
        }

        private static readonly Color ActiveTabColor = new Color(0.957f, 0.925f, 0.847f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.72f, 0.65f, 0.53f, 1f);

        private void SetTab(ScreenTab tab)
        {
            currentTab = tab;
            if (VerseListRoot != null) VerseListRoot.SetActive(tab == ScreenTab.Verses);
            if (ChapterListRoot != null) ChapterListRoot.SetActive(tab == ScreenTab.Chapters);
            if (BookListRoot != null) BookListRoot.SetActive(tab == ScreenTab.Books);
            // MultiplierButton's active state is fully managed in Refresh() now (it's dual-purpose:
            // bulk-buy cycle on Verses, free Unlock Chapter on Chapters-while-gated) - no static
            // per-tab default to set here anymore.
            if (VersesTabBackground != null) VersesTabBackground.color = tab == ScreenTab.Verses ? ActiveTabColor : InactiveTabColor;
            if (ChaptersTabBackground != null) ChaptersTabBackground.color = tab == ScreenTab.Chapters ? ActiveTabColor : InactiveTabColor;
            if (BooksTabBackground != null) BooksTabBackground.color = tab == ScreenTab.Books ? ActiveTabColor : InactiveTabColor;
            // Clicking the Verses tab directly always goes back to the live in-progress chapter -
            // only ReviewChapter() (from clicking a Chapters-tab row) leaves a specific chapter
            // pinned for review.
            if (tab == ScreenTab.Verses && VerseList != null)
            {
                VerseList.ReviewChapterNumber = -1;
                VerseList.ForceRefresh();
            }
            Refresh();
        }

        /// <summary>Called by ChapterListUI when the player clicks a chapter row - switches to the
        /// Verses tab showing that chapter's verses specifically, instead of always the live
        /// current chapter (2026-08-04, real gap: chapter rows were entirely unclickable before
        /// this, so there was no way to go back and re-read an earlier chapter's verses).</summary>
        public void ReviewChapter(int chapterNumber)
        {
            currentTab = ScreenTab.Verses;
            if (VerseListRoot != null) VerseListRoot.SetActive(true);
            if (ChapterListRoot != null) ChapterListRoot.SetActive(false);
            if (BookListRoot != null) BookListRoot.SetActive(false);
            if (MultiplierButton != null) MultiplierButton.gameObject.SetActive(true);
            if (VersesTabBackground != null) VersesTabBackground.color = ActiveTabColor;
            if (ChaptersTabBackground != null) ChaptersTabBackground.color = InactiveTabColor;
            if (BooksTabBackground != null) BooksTabBackground.color = InactiveTabColor;
            if (VerseList != null)
            {
                VerseList.ReviewChapterNumber = chapterNumber;
                VerseList.ForceRefresh();
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void HandleBuy()
        {
            AudioManager.Instance?.PlayPurchaseClick();
            if (showingChapters) Controller?.BuyNextChapter();
            // 2026-08-05, real bug fix: this used to just show a disabled "go unlock it on the
            // Chapters tab" message - the player had no way to actually open the gate from here.
            // Now it performs the free unlock directly, right where the player already is.
            else if (Controller != null && Controller.RequiresChapterUnlock) Controller.UnlockCurrentChapter();
            else Controller?.BuyVersesBulk();
        }

        /// <summary>MultiplierButton is dual-purpose: on the Verses tab it cycles the bulk-buy
        /// quantity (1x/2x/3x/4x/Max); on the Chapters tab, while the current chapter's gate is
        /// still closed, it becomes a free "Unlock Chapter" action instead - so the Complete
        /// Chapter bulk-buy-everything button isn't the ONLY way to get into a fresh chapter. Only
        /// shown then (see Refresh's MultiplierButton.gameObject.SetActive logic).</summary>
        private void HandleMultiplierOrUnlockClick()
        {
            if (showingChapters) Controller?.UnlockCurrentChapter();
            else Controller?.CycleVerseBuyMultiplier();
        }

        /// <summary>Maps the actual bulk quantity (1/5/10/20) to the "1x/2x/3x/4x" tier label the
        /// user asked for, rather than showing the literal quantity as a confusing "x" value.</summary>
        private static string MultiplierTierLabel(int quantity) => quantity switch
        {
            1 => "1x",
            5 => "2x",
            10 => "3x",
            20 => "4x",
            GameLoopController.MaxBuyMultiplier => "Max",
            _ => $"{quantity}x"
        };

        /// <summary>Called by VerseListUI when the player picks a verse to review. Only unlocked
        /// verses are selectable - the list itself disables the row's button for locked ones.</summary>
        public void SelectVerse(int index)
        {
            selectedVerseIndex = index;
            Refresh();
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Wallet == null) return;

            // Books tab (Phase F3, 2026-08-06) stays fully hidden - not just grayed out - until the
            // player has prestiged at least once, same progressive-disclosure pattern already used
            // for the ClickerScreen Skill Tree button (see ClickerScreenUI.Refresh).
            bool booksUnlocked = Controller.Prestige.PrestigeCount > 0;
            if (BooksTabButton != null) BooksTabButton.gameObject.SetActive(booksUnlocked);
            if (!booksUnlocked && currentTab == ScreenTab.Books) SetTab(ScreenTab.Verses);

            InkLabel.text = $"Ink: {NumberFormatter.Format(Controller.Wallet.Balance)}";

            var levels = Controller.Levels;

            // Triple-lane bar (2026-08-08) - same book/XP/Old Testament progression display as
            // ClickerScreenUI.Refresh, mirrored here per the user's explicit ask to match progression
            // UI across both screens. XpBar above stays wired (harmless if its GameObject is
            // disabled in-scene) so this isn't a behavior change if that swap is ever reverted.
            void SetLane(Image fill, TMP_Text label, float fraction, string text)
            {
                if (fill != null)
                {
                    var rt = fill.rectTransform;
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
                }
                if (label != null) label.text = text;
            }

            float xpFraction = levels.XpRequiredForNextLevel > 0
                ? (float)levels.XpIntoCurrentLevel / levels.XpRequiredForNextLevel
                : 0f;
            SetLane(XpLaneFill, XpLaneLabel, xpFraction, $"XP: Lv {levels.CurrentLevel} ({xpFraction * 100f:F0}%)");
            SetLane(BookLaneFill, BookLaneLabel, Controller.ActiveBookProgressFraction,
                $"{Controller.Verses?.BookName ?? "Book"}: {Controller.ActiveBookProgressFraction * 100f:F0}%");
            SetLane(OtLaneFill, OtLaneLabel, Controller.OldTestamentProgressFraction,
                $"Old Testament: {Controller.OldTestamentProgressFraction * 100f:F1}%");

            // Default to the most recently unlocked verse until the player picks something else
            // from the list - clamp in case a previously-selected index is somehow out of range.
            int displayIndex = selectedVerseIndex;
            if (displayIndex < 0 || displayIndex >= Controller.NextVerseIndex)
                displayIndex = Controller.NextVerseIndex - 1;

            // Refresh() runs every frame (passive Ink ticking fires OnStateChanged continuously),
            // but the displayed verse only actually changes on a discrete action (buy/select) -
            // gate the text rebuild + force-refresh on that, not every frame. Running
            // ForceFullRefresh/DeferredFullRefresh unconditionally here was a real contributor to
            // bug #22's lag (a new coroutine started every single frame).
            if (displayIndex != lastDisplayedIndex)
            {
                lastDisplayedIndex = displayIndex;

                if (displayIndex >= 0 && Controller.Verses.HasVerse(displayIndex))
                {
                    var verse = Controller.Verses.GetVerse(displayIndex);
                    ReferenceLabel.text = $"{Controller.Verses.BookName} {verse.Reference}";
                    VerseText.text = verse.Text;
                }
                else
                {
                    ReferenceLabel.text = "No verse unlocked yet";
                    VerseText.text = "Buy the first verse to reveal it here.";
                }

                // TMP's own dirty-flagging doesn't reliably trigger a repaint for text objects
                // built via editor script (same underlying issue seen on the scribe/verse list
                // rows) - force mesh/layout explicitly on the frame the displayed verse changes,
                // and reset scroll to the top so a shorter verse doesn't stay scrolled past its
                // own end.
                UiRefreshUtil.ForceFullRefresh(VerseText.rectTransform);
                StartCoroutine(UiRefreshUtil.DeferredFullRefresh(VerseText.rectTransform));
                if (VerseScrollRect != null) VerseScrollRect.verticalNormalizedPosition = 1f;
            }

            // Books tab has nothing to buy - switching happens per-row via BookListUI's own Switch
            // buttons - so the shared Buy/Multiplier controls just hide entirely there instead of
            // showing a stale Verses/Chapters action that doesn't apply.
            if (currentTab == ScreenTab.Books)
            {
                if (MultiplierButton != null) MultiplierButton.gameObject.SetActive(false);
                BuyButton.gameObject.SetActive(false);
                StatusLabel.text = "Choose a book below to switch your active reading progress.";
                return;
            }
            BuyButton.gameObject.SetActive(true);

            if (showingChapters)
            {
                // Dual-purpose slot: while this chapter's gate is still closed, it's a free
                // "Unlock Chapter" action (see HandleMultiplierOrUnlockClick) - once the gate is
                // open (either via this button or via Complete Chapter), it has nothing left to do
                // and hides, same as it would on the Verses tab once you're mid-chapter.
                bool showUnlock = Controller.RequiresChapterUnlock;
                if (MultiplierButton != null) MultiplierButton.gameObject.SetActive(showUnlock);
                if (MultiplierButtonLabel != null && showUnlock) MultiplierButtonLabel.text = "Unlock Chapter";
            }
            else
            {
                if (MultiplierButton != null) MultiplierButton.gameObject.SetActive(true);
                if (MultiplierButtonLabel != null)
                    MultiplierButtonLabel.text = MultiplierTierLabel(Controller.VerseBuyMultiplier);
            }

            if (Controller.BookComplete)
            {
                StatusLabel.text = "Book complete!";
                BuyButton.interactable = false;
                if (BuyButtonLabel != null) BuyButtonLabel.text = showingChapters ? "Complete Chapter" : "Buy Next Verse";
            }
            else if (showingChapters)
            {
                // "Complete Chapter" is the paid, all-at-once shortcut (buys every remaining verse
                // in the chapter at the bulk discount) - distinct from the free "Unlock Chapter"
                // button above, which only opens the gate so verses can be bought individually on
                // the Verses tab instead. 2026-08-05: previously this was the ONLY way to enter a
                // fresh chapter, and it silently bought every verse at once - defeating the point
                // of having a per-verse Verses tab at all.
                double chapterCost = Controller.ChapterBulkCost;
                int remaining = Controller.RemainingVersesInCurrentChapter;
                StatusLabel.text = $"Chapter {Controller.CurrentChapterNumber}: {remaining} verse(s) left to unlock";
                BuyButton.interactable = Controller.Wallet.Balance >= chapterCost;
                if (BuyButtonLabel != null)
                    BuyButtonLabel.text = $"Complete Chapter ({NumberFormatter.Format(chapterCost)} Ink)";
            }
            else if (Controller.RequiresChapterUnlock)
            {
                // 2026-08-05, real bug fix: this button now performs the free unlock directly
                // (see HandleBuy) instead of just displaying a disabled redirect message - no trip
                // to the Chapters tab required to start buying this chapter's verses individually.
                StatusLabel.text = $"Chapter {Controller.CurrentChapterNumber} is locked.";
                BuyButton.interactable = true;
                if (BuyButtonLabel != null) BuyButtonLabel.text = $"Unlock Chapter {Controller.CurrentChapterNumber}";
            }
            else
            {
                double bulkCost = Controller.VerseBulkCost;
                StatusLabel.text = $"Next verse unlocks at: {NumberFormatter.Format(Controller.NextVerseCost)} Ink";
                BuyButton.interactable = Controller.Wallet.Balance >= bulkCost;
                if (BuyButtonLabel != null)
                {
                    string quantityLabel = Controller.VerseBuyMultiplier == GameLoopController.MaxBuyMultiplier
                        ? "Max Verses"
                        : Controller.VerseBuyMultiplier == 1 ? "Next Verse" : $"{Controller.VerseBuyMultiplier} Verses";
                    BuyButtonLabel.text = $"Buy {quantityLabel} ({NumberFormatter.Format(bulkCost)} Ink)";
                }
            }
        }
    }
}
