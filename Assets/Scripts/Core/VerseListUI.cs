using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Builds one row per verse in the CURRENT chapter only (both already-bought and the
    /// not-yet-bought remainder of that chapter) - verses from earlier or later chapters are not
    /// listed at all until they become the current chapter. Rows are rebuilt whenever the current
    /// chapter changes, not just once in Awake, since the row count/content differs per chapter.
    /// </summary>
    public class VerseListUI : MonoBehaviour
    {
        private class Row
        {
            public TMP_Text ReferenceText;
            public TMP_Text CostText;
            public Button SelectButton;
        }

        public Transform Content;
        public GameObject RowTemplate;
        public BuyVerseScreenUI ScreenUi;

        /// <summary>-1 = show the live current in-progress chapter (default). Set by
        /// BuyVerseScreenUI.ReviewChapter() when the player clicks a chapter row on the Chapters
        /// tab to review a specific (usually already-completed) chapter's verses instead.</summary>
        public int ReviewChapterNumber = -1;

        private readonly List<Row> rows = new List<Row>();
        private int builtChapter = int.MinValue;
        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (Controller != null) Controller.OnStateChanged += Refresh;
            Refresh();
            ForceLayoutRebuild();
            // See ScribeListUI.Awake for why this coroutine follow-up is needed in addition to the
            // immediate call above.
            StartCoroutine(UiRefreshUtil.DeferredFullRefresh(Content));
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        /// <summary>Called by BuyVerseScreenUI.ReviewChapter() right after changing
        /// ReviewChapterNumber - setting that field alone doesn't repaint anything, since Refresh()
        /// otherwise only runs on GameLoopController.OnStateChanged (which a chapter-review click
        /// doesn't fire, since no wallet/verse state actually changed).</summary>
        public void ForceRefresh() => Refresh();

        /// <summary>
        /// See ScribeListUI.ForceLayoutRebuild for the two issues this works around: runtime-
        /// instantiated rows under a VerticalLayoutGroup don't reliably get an automatic layout
        /// pass, and this project's VerticalLayoutGroup width calculation was independently
        /// observed to be wrong - so width/position are forced directly via a full-stretch anchor
        /// rather than trusted to the group. Only direct children of Content (the rows) are
        /// touched, not their descendants - see ScribeListUI's comment for the row-corrupting bug
        /// that recursing into descendants caused.
        /// </summary>
        private void ForceLayoutRebuild()
        {
            if (Content == null) return;
            var contentRt = Content as RectTransform;
            if (contentRt == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

            for (int i = 0; i < Content.childCount; i++)
            {
                var rt = Content.GetChild(i) as RectTransform;
                if (rt == null) continue;
                rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
                rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
                rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            }

            UiRefreshUtil.ForceFullRefresh(contentRt);
        }

        private void RebuildRowsForCurrentChapter(int chapter)
        {
            foreach (var row in rows)
                if (row.SelectButton != null) Destroy(row.SelectButton.gameObject);
            rows.Clear();

            if (chapter < 0) return;

            int start = Controller.GetChapterStartIndex(chapter);
            if (start < 0) return;
            int end = start + Controller.GetChapterVerseCount(chapter);
            for (int i = start; i < end; i++)
            {
                int index = i;
                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.SetActive(true);
                rowGo.name = $"VerseRow_{i}";

                var row = new Row
                {
                    ReferenceText = rowGo.transform.Find("Reference").GetComponent<TMP_Text>(),
                    CostText = rowGo.transform.Find("Cost").GetComponent<TMP_Text>(),
                    SelectButton = rowGo.GetComponent<Button>()
                };

                row.SelectButton.onClick.AddListener(() => ScreenUi.SelectVerse(index));
                rows.Add(row);
            }
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Verses == null) return;

            int chapter = ReviewChapterNumber >= 0
                ? ReviewChapterNumber
                : (Controller.BookComplete ? -1 : Controller.CurrentChapterNumber);
            if (chapter != builtChapter)
            {
                RebuildRowsForCurrentChapter(chapter);
                builtChapter = chapter;
                ForceLayoutRebuild();
            }

            int start = chapter < 0 ? 0 : Controller.GetChapterStartIndex(chapter);
            for (int r = 0; r < rows.Count; r++)
            {
                int i = start + r;
                var row = rows[r];
                var verse = Controller.Verses.GetVerse(i);
                bool unlocked = i < Controller.NextVerseIndex;

                // Include the book name, not just "1:1" - a bare chapter:verse number reads as
                // meaningless without context, especially once multiple books are in play.
                string fullReference = $"{Controller.Verses.BookName} {verse.Reference}";

                if (unlocked)
                {
                    row.ReferenceText.text = fullReference;
                    row.CostText.text = "";
                    row.SelectButton.interactable = true;
                }
                else
                {
                    row.ReferenceText.text = $"{fullReference} [Locked]";
                    row.CostText.text = $"{NumberFormatter.Format(Controller.VerseCostAt(i))} Ink";
                    row.SelectButton.interactable = false;
                }
            }

            // See ScribeListUI.Refresh for why ForceFullRefresh is NOT called here on every
            // Refresh() - this runs every frame (passive Ink ticking) and the forced full
            // rebuild was the real cause of bug #22's lag. It's still called via
            // ForceLayoutRebuild() above, but only on the (rare) frame the row set actually
            // changes.
        }
    }
}
