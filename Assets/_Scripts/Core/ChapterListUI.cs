using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Builds one row per chapter in the current book, showing whether it's complete, in
    /// progress (partially bought), or locked - complements VerseListUI's per-verse view.
    /// Since verses are always bought in strict canonical order, every chapter before the
    /// current one is always fully complete and every chapter after it is always fully locked;
    /// only the single chapter containing NextVerseIndex can be "in progress".
    /// </summary>
    public class ChapterListUI : MonoBehaviour
    {
        private class Row
        {
            public int ChapterNumber;
            public int StartIndex;
            public int VerseCount;
            public TMP_Text ReferenceText;
            public TMP_Text CostText;
            public Button Button;
        }

        public Transform Content;
        public GameObject RowTemplate;
        public BuyVerseScreenUI ScreenUi;

        private readonly List<Row> rows = new List<Row>();
        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (Controller != null) Controller.OnStateChanged += Refresh;
            BuildRows();
            Refresh();
            ForceLayoutRebuild();
            StartCoroutine(UiRefreshUtil.DeferredFullRefresh(Content));
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        /// <summary>See ScribeListUI.ForceLayoutRebuild - same fix, only direct row children are
        /// touched, not their descendants.</summary>
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

        private void BuildRows()
        {
            if (Controller == null || Controller.Verses == null) return;

            Row current = null;
            for (int i = 0; i < Controller.Verses.VerseCount; i++)
            {
                var verse = Controller.Verses.GetVerse(i);
                if (current == null || verse.ChapterNumber != current.ChapterNumber)
                {
                    var rowGo = Instantiate(RowTemplate, Content);
                    rowGo.SetActive(true);
                    rowGo.name = $"ChapterRow_{verse.ChapterNumber}";

                    current = new Row
                    {
                        ChapterNumber = verse.ChapterNumber,
                        StartIndex = i,
                        VerseCount = 0,
                        ReferenceText = rowGo.transform.Find("Reference").GetComponent<TMP_Text>(),
                        CostText = rowGo.transform.Find("Cost").GetComponent<TMP_Text>(),
                        Button = rowGo.GetComponent<Button>()
                    };

                    int chapterNumber = current.ChapterNumber;
                    if (current.Button != null) current.Button.onClick.AddListener(() => ScreenUi?.ReviewChapter(chapterNumber));

                    rows.Add(current);
                }
                current.VerseCount++;
            }
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Verses == null) return;

            int currentChapter = Controller.CurrentChapterNumber;

            foreach (var row in rows)
            {
                string bookName = Controller.Verses.BookName;
                if (row.ChapterNumber < currentChapter || (currentChapter == -1 && Controller.BookComplete))
                {
                    row.ReferenceText.text = $"{bookName} Chapter {row.ChapterNumber}";
                    row.CostText.text = "Complete";
                    if (row.Button != null) row.Button.interactable = true; // reviewable via ReviewChapter
                }
                else if (row.ChapterNumber == currentChapter && !Controller.RequiresChapterUnlock)
                {
                    // Only reachable for the book's very first chapter - every later chapter is
                    // entered atomically via BuyNextChapter, so "in progress" (partially bought)
                    // never applies to them. See RequiresChapterUnlock.
                    int remaining = Controller.RemainingVersesInCurrentChapter;
                    row.ReferenceText.text = $"{bookName} Chapter {row.ChapterNumber} [In Progress]";
                    row.CostText.text = $"{NumberFormatter.Format(Controller.ChapterBulkCost)} Ink ({remaining} left)";
                    if (row.Button != null) row.Button.interactable = true; // has bought verses to review
                }
                else
                {
                    double cost = 0;
                    for (int j = 0; j < row.VerseCount; j++)
                        cost += Controller.VerseCostAt(row.StartIndex + j);
                    cost *= 0.75; // matches the documented chapter bulk-buy discount

                    row.ReferenceText.text = $"{bookName} Chapter {row.ChapterNumber} [Locked]";
                    row.CostText.text = $"{NumberFormatter.Format(cost)} Ink";
                    if (row.Button != null) row.Button.interactable = false; // nothing bought yet to review
                }
            }

            UiRefreshUtil.ForceFullRefresh(Content);
        }
    }
}
