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

        private void RebuildRowsForCurrentChapter()
        {
            foreach (var row in rows)
                if (row.SelectButton != null) Destroy(row.SelectButton.gameObject);
            rows.Clear();

            if (Controller.BookComplete) return;

            int start = Controller.CurrentChapterStartIndex;
            int end = Controller.CurrentChapterEndIndexExclusive;
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

            int chapter = Controller.BookComplete ? -1 : Controller.CurrentChapterNumber;
            if (chapter != builtChapter)
            {
                RebuildRowsForCurrentChapter();
                builtChapter = chapter;
                ForceLayoutRebuild();
            }

            int start = Controller.BookComplete ? 0 : Controller.CurrentChapterStartIndex;
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
