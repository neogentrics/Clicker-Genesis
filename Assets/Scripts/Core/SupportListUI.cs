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
            public TMP_Text NameText;
            public TMP_Text DescText;
            public TMP_Text CostText;
            public Button BuyButton;
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
                        NameText = rowGo.transform.Find("Name").GetComponent<TMP_Text>(),
                        DescText = rowGo.transform.Find("Desc").GetComponent<TMP_Text>(),
                        CostText = rowGo.transform.Find("BuyButton/CostText").GetComponent<TMP_Text>(),
                        BuyButton = rowGo.transform.Find("BuyButton").GetComponent<Button>()
                    };
                    EnableCostTextAutoSize(row.CostText);
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

        private string DescribeGenesisVerse(int verseIndex)
        {
            var db = Controller.GenesisVerseDatabase;
            if (db == null || !db.HasVerse(verseIndex)) return $"verse {verseIndex + 1}";
            var v = db.GetVerse(verseIndex);
            return $"Genesis {v.Reference}";
        }

        private static string ReqLine(bool satisfied, string metText, string unmetText)
        {
            return satisfied ? $"<color=#2E7D46>{metText}</color>" : $"<color=#D4372A>{unmetText}</color>";
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Scribes == null) return;
            var scribes = Controller.Scribes;

            foreach (var row in rows)
            {
                var subDef = scribes.GetSubmanagerDefinition(row.TierIndex, row.SubIndex);
                var parentDef = scribes.GetDefinition(row.TierIndex);
                bool owned = scribes.IsSubmanagerOwned(row.TierIndex, row.SubIndex);

                row.NameText.text = $"{subDef.displayName} ({parentDef.managerName})";

                bool parentOwned = scribes.IsManagerUnlocked(row.TierIndex);
                bool verseReached = Controller.GenesisNextVerseIndex >= subDef.unlockAtVerseIndex;
                bool allRequirementsMet = parentOwned && verseReached;

                if (owned)
                {
                    string perkLabel = subDef.perkFlavor == "cost-cutter"
                        ? $"-{subDef.perkAmount * 100:F0}% {parentDef.displayName} cost"
                        : $"+{subDef.perkAmount * 100:F0}% {parentDef.managerName}'s bonus";
                    row.DescText.text = perkLabel;
                }
                else if (!allRequirementsMet)
                {
                    var lines = new List<string>
                    {
                        ReqLine(parentOwned, $"{parentDef.managerName} hired", $"Requires {parentDef.managerName}"),
                        ReqLine(verseReached,
                            $"{DescribeGenesisVerse(subDef.unlockAtVerseIndex)} reached",
                            $"Reach {DescribeGenesisVerse(subDef.unlockAtVerseIndex)}")
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
                else
                {
                    row.CostText.text = subDef.unlockCost <= 0 ? "Free" : $"{NumberFormatter.Format(subDef.unlockCost)} Ink";
                    row.BuyButton.interactable = allRequirementsMet
                        && (subDef.unlockCost <= 0 || Controller.Wallet.Balance >= subDef.unlockCost);
                }
            }
        }
    }
}
