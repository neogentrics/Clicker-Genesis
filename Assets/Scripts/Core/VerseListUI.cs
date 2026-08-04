using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Builds one row per verse in the current book (all of them, not just unlocked ones) so the
    /// player can review any previously unlocked verse and see upcoming locked verses' unlock
    /// costs ahead of time. Rows are built once in Awake; Refresh() only updates text/interactable
    /// state, it doesn't rebuild the hierarchy - same shape as ScribeListUI.
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
        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (Controller != null) Controller.OnStateChanged += Refresh;
            BuildRows();
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

        private void BuildRows()
        {
            if (Controller == null || Controller.Verses == null) return;

            for (int i = 0; i < Controller.Verses.VerseCount; i++)
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

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
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

            UiRefreshUtil.ForceFullRefresh(Content);
        }
    }
}
