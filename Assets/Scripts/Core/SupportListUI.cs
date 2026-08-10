using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Support tab (2026-08-06, task #161) - lists every submanager across every scribe tier in
    /// its own tab, instead of nested under each manager's row on the Managers tab. Same row shape
    /// and same color-coded multi-requirement Desc pattern as ManagerListUI (task #160) - built as
    /// its own component rather than reusing ManagerListUI directly since submanagers are no longer
    /// tied to a parent row's layout position once they have a tab of their own.
    /// </summary>
    public class SupportListUI : MonoBehaviour
    {
        private class Row
        {
            public int TierIndex;
            public int SubIndex;
            public Image Icon;
            public TMP_Text NameText;
            public TMP_Text DescText;
            public TMP_Text CostText;
            public Button BuyButton;
        }

        public Transform Content;
        public GameObject RowTemplate;

        private readonly List<Row> rows = new List<Row>();
        private GameLoopController Controller => GameLoopController.Instance;

        /// <summary>See ScribeListUI's field of the same name - which book's roster `rows` were
        /// built from (2026-08-08, multi-book economy).</summary>
        private string builtForBookId;

        private void Awake()
        {
            if (Controller != null) Controller.OnStateChanged += Refresh;
            RebuildRows();
            ForceLayoutRebuild();
            StartCoroutine(UiRefreshUtil.DeferredFullRefresh(Content));
        }

        /// <summary>See ScribeListUI.RebuildRows for why this exists and why RowTemplate is
        /// skipped when clearing Content's children.</summary>
        private void RebuildRows()
        {
            rows.Clear();
            if (Content != null)
                for (int i = Content.childCount - 1; i >= 0; i--)
                {
                    var child = Content.GetChild(i).gameObject;
                    if (child != RowTemplate) Destroy(child);
                }
            BuildRows();
            builtForBookId = Controller != null ? Controller.ActiveBookResourceId : null;
            Refresh();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        /// <summary>See ScribeListUI.ForceLayoutRebuild for why this exists and why it must only
        /// touch direct children of Content, not descendants.</summary>
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
            if (Controller == null || Controller.Scribes == null) return;
            var scribes = Controller.Scribes;

            for (int i = 0; i < scribes.TierCount; i++)
            {
                for (int s = 0; s < scribes.SubmanagerCount(i); s++)
                {
                    int tierIndex = i;
                    int subIndex = s;
                    var subDef = scribes.GetSubmanagerDefinition(tierIndex, subIndex);

                    var rowGo = Instantiate(RowTemplate, Content);
                    rowGo.SetActive(true);
                    rowGo.name = $"SupportRow_{subDef.id}";

                    var row = new Row
                    {
                        TierIndex = tierIndex,
                        SubIndex = subIndex,
                        Icon = rowGo.transform.Find("Icon").GetComponent<Image>(),
                        NameText = rowGo.transform.Find("Name").GetComponent<TMP_Text>(),
                        DescText = rowGo.transform.Find("Desc").GetComponent<TMP_Text>(),
                        CostText = rowGo.transform.Find("BuyButton/CostText").GetComponent<TMP_Text>(),
                        BuyButton = rowGo.transform.Find("BuyButton").GetComponent<Button>()
                    };
                    EnableCostTextAutoSize(row.CostText);
                    // Icon now comes straight from data (2026-08-08, replaces the old per-book
                    // hardcoded C# switch) - SubmanagerDefinition.icon is set per-submanager in
                    // each book's ScribeSetConfig asset.
                    row.Icon.sprite = subDef.icon;
                    row.Icon.color = row.Icon.sprite != null ? Color.white : new Color(0, 0, 0, 0);
                    row.BuyButton.onClick.AddListener(() => Controller.BuySubmanager(tierIndex, subIndex));
                    rows.Add(row);
                }
            }
        }

        private static void EnableCostTextAutoSize(TMP_Text text)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(12f, text.fontSize * 0.4f);
            text.fontSizeMax = text.fontSize;
        }

        /// <summary>Converts a verse index (within the active book) into a human "BookName X:Y"
        /// reference - generalized 2026-08-08 for multi-book, was hardcoded to "Genesis" before
        /// Exodus existed.</summary>
        private string DescribeVerse(int verseIndex)
        {
            var db = Controller.Verses;
            if (db == null || !db.HasVerse(verseIndex)) return $"verse {verseIndex + 1}";
            var v = db.GetVerse(verseIndex);
            return $"{db.BookName} {v.Reference}";
        }

        private static string ReqLine(bool satisfied, string metText, string unmetText)
        {
            return satisfied ? $"<color=#2E7D46>{metText}</color>" : $"<color=#D4372A>{unmetText}</color>";
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Scribes == null) return;

            // See ScribeListUI.Refresh's identical check for why (2026-08-08, multi-book economy).
            if (Controller.ActiveBookResourceId != builtForBookId)
            {
                RebuildRows();
                ForceLayoutRebuild();
                return;
            }

            var scribes = Controller.Scribes;

            foreach (var row in rows)
            {
                var subDef = scribes.GetSubmanagerDefinition(row.TierIndex, row.SubIndex);
                var parentDef = scribes.GetDefinition(row.TierIndex);
                bool owned = scribes.IsSubmanagerOwned(row.TierIndex, row.SubIndex);

                // 2026-08-09, real bug fix: a Law tier never has a managerName (there's no
                // person to name), so a submanager attached to one (e.g. Jezrahiah under
                // Nehemiah's "The Wall Dedication") rendered as "Jezrahiah ()" - fall back to
                // the tier's own displayName, which always exists.
                string parentLabel = parentDef.HasManager ? parentDef.managerName : parentDef.displayName;
                row.NameText.text = $"{subDef.displayName} ({parentLabel})";

                // Parent-ready condition mirrors ScribeSystem.CanBuySubmanager (2026-08-09):
                // a Person-tier parent must have its manager bought; a Law-tier parent has no
                // such purchase step, so "ready" means the law tier itself has been reached.
                bool parentReady = parentDef.HasManager
                    ? scribes.IsManagerUnlocked(row.TierIndex)
                    : scribes.IsUnlocked(row.TierIndex, Controller.NextVerseIndex);
                bool verseReached = Controller.NextVerseIndex >= subDef.unlockAtVerseIndex;
                bool allRequirementsMet = parentReady && verseReached;

                if (owned)
                {
                    string perkLabel = subDef.isBookWidePatron
                        ? $"+{subDef.perkAmount * 100:F0}% output to every law tier in this book"
                        : subDef.perkFlavor == "cost-cutter"
                            ? $"-{subDef.perkAmount * 100:F0}% {parentDef.displayName} cost"
                            : $"+{subDef.perkAmount * 100:F0}% {parentLabel}'s bonus";
                    row.DescText.text = perkLabel;
                }
                else if (!allRequirementsMet)
                {
                    var lines = new List<string>
                    {
                        ReqLine(parentReady, $"{parentLabel} ready", $"Requires {parentLabel}"),
                        ReqLine(verseReached,
                            $"{DescribeVerse(subDef.unlockAtVerseIndex)} reached",
                            $"Reach {DescribeVerse(subDef.unlockAtVerseIndex)}")
                    };
                    row.DescText.text = string.Join("\n", lines);
                }
                else
                {
                    row.DescText.text = "";
                }

                if (owned)
                {
                    row.CostText.text = "Hired";
                    row.BuyButton.interactable = false;
                }
                else if (!allRequirementsMet)
                {
                    // 2026-08-09, same real fix as ManagerListUI - never show a real price until
                    // every requirement line above it is actually satisfied.
                    row.CostText.text = "Locked";
                    row.BuyButton.interactable = false;
                }
                else
                {
                    row.CostText.text = subDef.unlockCost <= 0 ? "Free" : $"{NumberFormatter.Format(subDef.unlockCost)} Ink";
                    row.BuyButton.interactable = subDef.unlockCost <= 0 || Controller.Wallet.Balance >= subDef.unlockCost;
                }
            }
        }
    }
}
