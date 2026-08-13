using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClickerGenesis.Data;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Books tab (Phase F3, 2026-08-06): one row per canonical OT book (Genesis first), showing
    /// unlock/progress status and a Switch button. The whole tab (button + list, wired in
    /// BuyVerseScreenUI) is hidden entirely until the player has prestiged at least once - same
    /// progressive-disclosure pattern as the ClickerScreen Skill Tree button. Roster is static
    /// (CanonicalBookOrder.Books never changes at runtime), so rows are built once in Awake, same
    /// shape as ChapterListUI/ManagerListUI.
    /// </summary>
    public class BookListUI : MonoBehaviour
    {
        private class Row
        {
            public string ResourceId;
            public TMP_Text ReferenceText;
            public TMP_Text CostText;
            public Button Button;
            public HoverTooltip Tooltip;
        }

        public Transform Content;
        public GameObject RowTemplate;

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

        /// <summary>See ChapterListUI.ForceLayoutRebuild - same fix, only direct row children are
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
            if (Controller == null) return;

            foreach (var (resourceId, displayName) in Controller.AllBooksInOrder)
            {
                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.SetActive(true);
                rowGo.name = $"BookRow_{resourceId}";

                string id = resourceId;
                var row = new Row
                {
                    ResourceId = id,
                    ReferenceText = rowGo.transform.Find("Reference").GetComponent<TMP_Text>(),
                    CostText = rowGo.transform.Find("Cost").GetComponent<TMP_Text>(),
                    Button = rowGo.GetComponent<Button>(),
                    // Tooltip explains HOW to unlock a locked book - added to every row (2026-08-06,
                    // real user report while testing) but only given text/enabled for the rows that
                    // actually need it (see Refresh) - "Switch"/"Reading now"/"Finish X first" rows
                    // are already self-explanatory without one.
                    Tooltip = rowGo.gameObject.AddComponent<HoverTooltip>()
                };
                if (row.Button != null) row.Button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayGenericClick();
                    Controller.SwitchActiveBook(id);
                });
                rows.Add(row);
            }

            BuildComingSoonRow();
        }

        /// <summary>Static row appended after the last canonical OT book (2026-08-08) - the New
        /// Testament's scribe/manager data is fully imported (all 27 books) but deliberately not
        /// exposed anywhere in gameplay yet (CanonicalBookOrder only lists the 39 OT books, so there
        /// is no code path to switch into or unlock an NT book) until the OT experience itself is
        /// polished. This row is purely informational - never added to `rows`, so Refresh() never
        /// touches it and its text/disabled state never changes.</summary>
        private void BuildComingSoonRow()
        {
            var rowGo = Instantiate(RowTemplate, Content);
            rowGo.SetActive(true);
            rowGo.name = "BookRow_NewTestamentComingSoon";

            var referenceText = rowGo.transform.Find("Reference").GetComponent<TMP_Text>();
            var costText = rowGo.transform.Find("Cost").GetComponent<TMP_Text>();
            var button = rowGo.GetComponent<Button>();

            referenceText.text = "New Testament";
            costText.text = "Coming Soon";
            if (button != null) button.interactable = false;

            var tooltip = rowGo.gameObject.AddComponent<HoverTooltip>();
            tooltip.enabled = true;
            tooltip.Text = "The New Testament's own book roster is being polished and isn't playable yet - hang tight!";
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Skills == null) return;

            foreach (var row in rows)
            {
                bool active = Controller.IsBookActive(row.ResourceId);
                bool complete = Controller.IsBookComplete(row.ResourceId);
                bool canSwitch = !active && Controller.CanSwitchToBook(row.ResourceId);
                string displayName = CanonicalBookOrder.DisplayNameOf(row.ResourceId);

                string suffix = active ? " [Active]" : complete ? " [Complete]" : "";
                row.ReferenceText.text = displayName + suffix;

                if (active)
                {
                    row.CostText.text = "Reading now";
                    row.Button.interactable = false;
                    row.Tooltip.enabled = false;
                }
                else if (canSwitch)
                {
                    row.CostText.text = "Switch";
                    row.Button.interactable = true;
                    row.Tooltip.enabled = false;
                }
                else
                {
                    // Every non-active, non-complete book is either unlocked (caught by canSwitch
                    // above) or not - there's no more "Finish X first" neighbor state (2026-08-09,
                    // player-choice book unlocking redesign removed the canonical-neighbor gate,
                    // see GameLoopController.CanSwitchToBook's doc comment).
                    row.CostText.text = "Locked";
                    row.Button.interactable = false;
                    row.Tooltip.enabled = true;
                    row.Tooltip.Text = "Can be unlocked in the Skill Tree after a prestige is done.";
                }
            }

            UiRefreshUtil.ForceFullRefresh(Content);
        }
    }
}
