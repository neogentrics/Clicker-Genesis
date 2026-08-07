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
    /// Row body simplified 2026-08-06 (user's explicit correction, relayed from a parallel
    /// session): no flavor text - just the manager's actual active bonus/perk, as a list of lines
    /// so a future perk source (a Grace Skill Tree node beyond the global manager-bonus boost
    /// already handled here) can append its own line without restructuring Refresh().
    /// Submanagers moved out to their own Support tab (2026-08-06, task #161, SupportListUI) -
    /// this component no longer builds or renders submanager sub-rows.
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
                EnableCostTextAutoSize(row.CostText);
                row.BuyButton.onClick.AddListener(() => Controller.BuyManager(tierIndex));
                rows.Add(row);
            }
        }

        private static void EnableCostTextAutoSize(TMP_Text text)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(12f, text.fontSize * 0.4f);
            text.fontSizeMax = text.fontSize;
        }

        /// <summary>Converts a Genesis-relative verse index into a human "Genesis X:Y" reference
        /// (2026-08-06) - falls back to a plain index if Genesis' own VerseDatabase isn't loaded
        /// yet for any reason.</summary>
        private string DescribeGenesisVerse(int verseIndex)
        {
            var db = Controller.GenesisVerseDatabase;
            if (db == null || !db.HasVerse(verseIndex)) return $"verse {verseIndex + 1}";
            var v = db.GetVerse(verseIndex);
            return $"Genesis {v.Reference}";
        }

        /// <summary>Color-codes one requirement line green (satisfied) or red (unsatisfied)
        /// (2026-08-06, task #160) - plain color only, no glyph prefix, since the project's Georgia
        /// TMP font asset is missing several Unicode symbols and silently renders them as tofu boxes
        /// (hit once already with "↳" on submanager rows).</summary>
        private static string ReqLine(bool satisfied, string metText, string unmetText)
        {
            return satisfied ? $"<color=#2E7D46>{metText}</color>" : $"<color=#D4372A>{unmetText}</color>";
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Scribes == null) return;
            var scribes = Controller.Scribes;
            int level = Controller.EffectiveManagerLevel;
            // Overseer's Wisdom (Grace Skill Tree) boosts every manager's output bonus - the row
            // must show the real effective total, not just the manager's own base percentage, or
            // the number here silently drifts from what GetTierInkPerSecond actually pays out.
            double skillBonusBoost = Controller.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ManagerBonusBoost);

            foreach (var row in rows)
            {
                var def = scribes.GetDefinition(row.TierIndex);
                row.NameText.text = def.managerName;

                bool active = scribes.IsManagerActive(row.TierIndex, level);
                bool unlocked = scribes.IsManagerUnlocked(row.TierIndex);
                bool levelEligible = scribes.IsManagerLevelEligible(row.TierIndex, level);
                // A manager tied to a scribe tier the player hasn't unlocked yet (verse progress)
                // must stay locked even if the level threshold is already met - fixes a real bug
                // where Noah could be bought before Ark's Manifest itself was unlocked. Genesis-
                // specific gating (not the active book's cursor) - see
                // GameLoopController.GenesisNextVerseIndex for why (Phase F book-switching, 2026-08-06).
                bool scribeTierUnlocked = scribes.IsUnlocked(row.TierIndex, Controller.GenesisNextVerseIndex);
                // The manager's own character verse (real CharacterIndex data, 2026-08-06) -
                // deliberately separate from scribeTierUnlocked above, see ScribeSystem's doc
                // comment on IsManagerVerseReached for why they can't share a field.
                bool managerVerseReached = scribes.IsManagerVerseReached(row.TierIndex, Controller.GenesisNextVerseIndex);
                bool allRequirementsMet = scribeTierUnlocked && managerVerseReached && levelEligible;

                // Redesigned 2026-08-06 (task #160, real user correction - the first pass only ever
                // showed ONE blocking reason since it was a sequential if/else chain, so every
                // manager past Adam displayed the wrong/incomplete reason). Now: while not yet
                // owned, show EVERY requirement as its own color-coded line (green=met, red=not) so
                // simultaneous requirements (scribe tier + manager verse + level) are all visible at
                // once. Once every requirement is met, the reason lines disappear entirely (nothing
                // to show yet - not bought, so no bonus either). Once owned/active, switch to the
                // real bonus lines. The button (CostText below) NEVER shows any of this reason text -
                // it only ever shows Owned / Free / a cost, per the user's explicit "the only thing
                // supposed to be here is cost" instruction (fixes bug #54's overflow as a byproduct).
                if (active)
                {
                    string desc = $"+{def.managerBonusMultiplier * 100:F0}% {def.displayName} output";
                    if (skillBonusBoost > 0)
                        desc += $"\n<color=#2E7D46>+{skillBonusBoost * 100:F0}% from Overseer's Wisdom</color>";
                    row.DescText.text = desc;
                }
                else if (!unlocked && !allRequirementsMet)
                {
                    var lines = new List<string>
                    {
                        ReqLine(scribeTierUnlocked, $"{def.displayName} unlocked", $"Unlock {def.displayName} first"),
                        ReqLine(managerVerseReached,
                            $"{DescribeGenesisVerse(def.managerUnlockAtVerseIndex)} reached",
                            $"Reach {DescribeGenesisVerse(def.managerUnlockAtVerseIndex)}"),
                        ReqLine(levelEligible, $"Level {def.managerUnlockLevel} reached", $"Reach level {def.managerUnlockLevel}")
                    };
                    row.DescText.text = string.Join("\n", lines);
                }
                else
                {
                    row.DescText.text = "";
                }

                if (unlocked)
                {
                    row.CostText.text = "Owned";
                    row.BuyButton.interactable = false;
                }
                else
                {
                    row.CostText.text = def.managerUnlockCost <= 0
                        ? "Free"
                        : $"{NumberFormatter.Format(def.managerUnlockCost)} Ink";
                    row.BuyButton.interactable = allRequirementsMet
                        && (def.managerUnlockCost <= 0 || Controller.Wallet.Balance >= def.managerUnlockCost);
                }
            }
        }
    }
}
