using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Managers tab (2026-08-04): lists only the scribe tiers that have a manager, with unlock
    /// status (Locked-by-level / Buyable / Active) and a Buy button. Managers require BOTH the
    /// level threshold AND an Ink cost (Adam, the first manager, is free once level-eligible) -
    /// this supersedes the earlier "auto-unlocks by level alone" design. Rows are built once in
    /// Awake (manager roster is static per book), same shape as ScribeListUI.
    /// </summary>
    public class ManagerListUI : MonoBehaviour
    {
        private class Row
        {
            public int TierIndex;
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
                var def = scribes.GetDefinition(i);
                if (!def.HasManager) continue;

                int tierIndex = i;
                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.SetActive(true);
                rowGo.name = $"ManagerRow_{def.managerName}";

                var row = new Row
                {
                    TierIndex = tierIndex,
                    NameText = rowGo.transform.Find("Name").GetComponent<TMP_Text>(),
                    DescText = rowGo.transform.Find("Desc").GetComponent<TMP_Text>(),
                    CostText = rowGo.transform.Find("BuyButton/CostText").GetComponent<TMP_Text>(),
                    BuyButton = rowGo.transform.Find("BuyButton").GetComponent<Button>()
                };
                row.BuyButton.onClick.AddListener(() => Controller.BuyManager(tierIndex));
                rows.Add(row);
            }
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Scribes == null) return;
            var scribes = Controller.Scribes;
            int level = Controller.EffectiveManagerLevel;

            foreach (var row in rows)
            {
                var def = scribes.GetDefinition(row.TierIndex);
                row.NameText.text = def.managerName;

                bool active = scribes.IsManagerActive(row.TierIndex, level);
                bool unlocked = scribes.IsManagerUnlocked(row.TierIndex);
                bool levelEligible = scribes.IsManagerLevelEligible(row.TierIndex, level);
                // A manager tied to a scribe tier the player hasn't unlocked yet (verse progress)
                // must stay locked even if the level threshold is already met - fixes a real bug
                // where Noah could be bought before Ark's Manifest itself was unlocked.
                bool scribeTierUnlocked = scribes.IsUnlocked(row.TierIndex, Controller.NextVerseIndex);

                string desc = string.IsNullOrEmpty(def.managerFlavorText)
                    ? $"Manages {def.displayName}."
                    : def.managerFlavorText;
                if (active)
                    desc += $"\n<b><color=#2E7D46>Active - +{def.managerBonusMultiplier * 100:F0}% {def.displayName} output.</color></b>";
                else if (unlocked)
                    desc += $"\n<color=#2E7D46>Unlocked - owns no {def.displayName} yet to manage.</color>";
                row.DescText.text = desc;

                if (unlocked)
                {
                    row.CostText.text = "Owned";
                    row.BuyButton.interactable = false;
                }
                else if (!scribeTierUnlocked)
                {
                    row.CostText.text = $"Unlocks {def.displayName} first";
                    row.BuyButton.interactable = false;
                }
                else if (!levelEligible)
                {
                    row.CostText.text = $"Reach level {def.managerUnlockLevel}";
                    row.BuyButton.interactable = false;
                }
                else
                {
                    row.CostText.text = def.managerUnlockCost <= 0
                        ? "Free"
                        : $"{NumberFormatter.Format(def.managerUnlockCost)} Ink";
                    row.BuyButton.interactable = def.managerUnlockCost <= 0 || Controller.Wallet.Balance >= def.managerUnlockCost;
                }
            }
        }
    }
}
