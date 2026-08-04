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

        [Header("XP bar (mirrors the one on ClickerScreen)")]
        public TMP_Text XpBarText;
        public Image XpBarFill;

        [Header("Verses/Chapters tabs")]
        public Button VersesTabButton;
        public Button ChaptersTabButton;
        public GameObject VerseListRoot;
        public GameObject ChapterListRoot;
        public Image VersesTabBackground;
        public Image ChaptersTabBackground;

        /// <summary>-1 = nothing selected yet (defaults to the most recently unlocked verse).</summary>
        private int selectedVerseIndex = -1;
        private bool showingChapters;

        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            // See ClickerScreenUI.Awake() for why this wiring lives here, not in Start().
            BuyButton.onClick.AddListener(HandleBuy);
            if (MultiplierButton != null) MultiplierButton.onClick.AddListener(() => Controller?.CycleVerseBuyMultiplier());
            if (VersesTabButton != null) VersesTabButton.onClick.AddListener(() => SetTab(false));
            if (ChaptersTabButton != null) ChaptersTabButton.onClick.AddListener(() => SetTab(true));
            if (Controller != null) Controller.OnStateChanged += Refresh;
            SetTab(false);
        }

        private static readonly Color ActiveTabColor = new Color(0.957f, 0.925f, 0.847f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.72f, 0.65f, 0.53f, 1f);

        private void SetTab(bool chapters)
        {
            showingChapters = chapters;
            if (VerseListRoot != null) VerseListRoot.SetActive(!chapters);
            if (ChapterListRoot != null) ChapterListRoot.SetActive(chapters);
            if (MultiplierButton != null) MultiplierButton.gameObject.SetActive(!chapters);
            if (VersesTabBackground != null) VersesTabBackground.color = chapters ? InactiveTabColor : ActiveTabColor;
            if (ChaptersTabBackground != null) ChaptersTabBackground.color = chapters ? ActiveTabColor : InactiveTabColor;
            Refresh();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void HandleBuy()
        {
            if (showingChapters) Controller?.BuyNextChapter();
            else Controller?.BuyVersesBulk();
        }

        /// <summary>Maps the actual bulk quantity (1/5/10/20) to the "1x/2x/3x/4x" tier label the
        /// user asked for, rather than showing the literal quantity as a confusing "x" value.</summary>
        private static string MultiplierTierLabel(int quantity) => quantity switch
        {
            1 => "1x",
            5 => "2x",
            10 => "3x",
            20 => "4x",
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

            InkLabel.text = $"Ink: {NumberFormatter.Format(Controller.Wallet.Balance)}";

            var levels = Controller.Levels;
            if (XpBarText != null) XpBarText.text = $"Level {levels.CurrentLevel} — {levels.XpIntoCurrentLevel}/{levels.XpRequiredForNextLevel} XP";
            if (XpBarFill != null)
                XpBarFill.fillAmount = levels.XpRequiredForNextLevel > 0
                    ? (float)levels.XpIntoCurrentLevel / levels.XpRequiredForNextLevel
                    : 0f;

            // Default to the most recently unlocked verse until the player picks something else
            // from the list - clamp in case a previously-selected index is somehow out of range.
            int displayIndex = selectedVerseIndex;
            if (displayIndex < 0 || displayIndex >= Controller.NextVerseIndex)
                displayIndex = Controller.NextVerseIndex - 1;

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

            // TMP's own dirty-flagging doesn't reliably trigger a repaint for text objects built
            // via editor script (same underlying issue seen on the scribe/verse list rows) -
            // force mesh/layout explicitly every time the displayed verse changes, and reset
            // scroll to the top so a shorter verse doesn't stay scrolled past its own end. The
            // immediate call is verified-correct in scripted testing only; the deferred coroutine
            // is what actually fixes it in real continuous Play (see UiRefreshUtil).
            UiRefreshUtil.ForceFullRefresh(VerseText.rectTransform);
            StartCoroutine(UiRefreshUtil.DeferredFullRefresh(VerseText.rectTransform));
            if (VerseScrollRect != null) VerseScrollRect.verticalNormalizedPosition = 1f;

            if (MultiplierButtonLabel != null)
                MultiplierButtonLabel.text = MultiplierTierLabel(Controller.VerseBuyMultiplier);

            if (Controller.BookComplete)
            {
                StatusLabel.text = "Book complete!";
                BuyButton.interactable = false;
                if (BuyButtonLabel != null) BuyButtonLabel.text = showingChapters ? "Buy Next Chapter" : "Buy Next Verse";
            }
            else if (showingChapters)
            {
                double chapterCost = Controller.ChapterBulkCost;
                int remaining = Controller.RemainingVersesInCurrentChapter;
                StatusLabel.text = $"Chapter {Controller.CurrentChapterNumber}: {remaining} verse(s) left to unlock";
                BuyButton.interactable = Controller.Wallet.Balance >= chapterCost;
                if (BuyButtonLabel != null)
                    BuyButtonLabel.text = $"Buy Next Chapter ({NumberFormatter.Format(chapterCost)} Ink)";
            }
            else
            {
                double bulkCost = Controller.VerseBulkCost;
                StatusLabel.text = $"Next verse unlocks at: {NumberFormatter.Format(Controller.NextVerseCost)} Ink";
                BuyButton.interactable = Controller.Wallet.Balance >= bulkCost;
                if (BuyButtonLabel != null)
                {
                    string quantityLabel = Controller.VerseBuyMultiplier == 1 ? "Next Verse" : $"{Controller.VerseBuyMultiplier} Verses";
                    BuyButtonLabel.text = $"Buy {quantityLabel} ({NumberFormatter.Format(bulkCost)} Ink)";
                }
            }
        }
    }
}
