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
    /// already handled here, or the not-yet-built submanager system) can append its own line
    /// without restructuring Refresh().
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

        /// <summary>Submanager sub-row (2026-08-06) - reuses the same RowTemplate as manager rows,
        /// indented under its parent manager, showing the character's own bonus/perk.</summary>
        private class SubRow
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
        private readonly List<SubRow> subRows = new List<SubRow>();
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
                // "Unlock X first" is longer than a plain cost like "500 Ink" - auto-size so long
                // locked-state text shrinks to fit instead of overflowing the button (2026-08-06,
                // same TMP auto-size fix used for the Skill Tree's "Unlock Chapter" button, bug #48).
                EnableCostTextAutoSize(row.CostText);
                row.BuyButton.onClick.AddListener(() => Controller.BuyManager(tierIndex));
                rows.Add(row);

                for (int s = 0; s < scribes.SubmanagerCount(i); s++)
                {
                    int subIndex = s;
                    var subDef = scribes.GetSubmanagerDefinition(tierIndex, subIndex);

                    var subGo = Instantiate(RowTemplate, Content);
                    subGo.SetActive(true);
                    subGo.name = $"SubmanagerRow_{subDef.id}";

                    var subRow = new SubRow
                    {
                        TierIndex = tierIndex,
                        SubIndex = subIndex,
                        NameText = subGo.transform.Find("Name").GetComponent<TMP_Text>(),
                        DescText = subGo.transform.Find("Desc").GetComponent<TMP_Text>(),
                        CostText = subGo.transform.Find("BuyButton/CostText").GetComponent<TMP_Text>(),
                        BuyButton = subGo.transform.Find("BuyButton").GetComponent<Button>()
                    };
                    // Smaller/indented so it visually reads as belonging to the manager row above it.
                    subRow.NameText.fontSize *= 0.85f;
                    EnableCostTextAutoSize(subRow.CostText);
                    subRow.BuyButton.onClick.AddListener(() => Controller.BuySubmanager(tierIndex, subIndex));
                    subRows.Add(subRow);
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

        /// <summary>Converts a Genesis-relative verse index into a human "Genesis X:Y" reference
        /// for locked-submanager text (2026-08-06) - falls back to a plain index if Genesis' own
        /// VerseDatabase isn't loaded yet for any reason.</summary>
        private string DescribeGenesisVerse(int verseIndex)
        {
            var db = Controller.GenesisVerseDatabase;
            if (db == null || !db.HasVerse(verseIndex)) return $"verse {verseIndex + 1}";
            var v = db.GetVerse(verseIndex);
            return $"Genesis {v.Reference}";
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

                // Row body is JUST the active bonus/perk now - no flavor text (2026-08-06, user's
                // explicit correction). Built as separate lines so a future perk source (a
                // per-manager skill node, or the not-yet-built submanager system) can append its
                // own line here without restructuring this method. Nothing shown at all until the
                // manager is actually active (bought AND has a scribe of its tier to manage) -
                // CostText below already explains why it isn't yet.
                if (active)
                {
                    string desc = $"+{def.managerBonusMultiplier * 100:F0}% {def.displayName} output";
                    if (skillBonusBoost > 0)
                        desc += $"\n<color=#2E7D46>+{skillBonusBoost * 100:F0}% from Overseer's Wisdom</color>";
                    row.DescText.text = desc;
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
                else if (!scribeTierUnlocked)
                {
                    // "Unlock" not "Unlocks" (2026-08-06, user correction) - reads as an instruction
                    // to the player, not a description of the button's own action.
                    row.CostText.text = $"Unlock {def.displayName} first";
                    row.BuyButton.interactable = false;
                }
                else if (!managerVerseReached)
                {
                    // The character's own real verse hasn't been reached yet, even though the
                    // scribe tier itself is already buyable (2026-08-06) - shows the actual
                    // reference, same "don't just give a raw number" fix as the scribe rows.
                    row.CostText.text = $"Unlocks at {DescribeGenesisVerse(def.managerUnlockAtVerseIndex)}";
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

            foreach (var sub in subRows)
            {
                var subDef = scribes.GetSubmanagerDefinition(sub.TierIndex, sub.SubIndex);
                var parentDef = scribes.GetDefinition(sub.TierIndex);
                bool owned = scribes.IsSubmanagerOwned(sub.TierIndex, sub.SubIndex);
                bool managerActive = scribes.IsManagerActive(sub.TierIndex, level);
                bool verseReached = Controller.GenesisNextVerseIndex >= subDef.unlockAtVerseIndex;

                // Plain ASCII prefix, not "↳" - the project's Georgia TMP font asset doesn't
                // include that glyph and rendered it as a tofu box (2026-08-06, caught live).
                sub.NameText.text = $"- {subDef.displayName}";

                string perkLabel = subDef.perkFlavor == "cost-cutter"
                    ? $"-{subDef.perkAmount * 100:F0}% {parentDef.displayName} cost"
                    : $"+{subDef.perkAmount * 100:F0}% {parentDef.managerName}'s bonus";
                sub.DescText.text = owned ? perkLabel : "";

                if (owned)
                {
                    sub.CostText.text = "Hired";
                    sub.BuyButton.interactable = false;
                }
                else if (!scribes.IsManagerUnlocked(sub.TierIndex))
                {
                    sub.CostText.text = $"Requires {parentDef.managerName}";
                    sub.BuyButton.interactable = false;
                }
                else if (!verseReached)
                {
                    sub.CostText.text = $"Unlocks at {DescribeGenesisVerse(subDef.unlockAtVerseIndex)}";
                    sub.BuyButton.interactable = false;
                }
                else
                {
                    sub.CostText.text = subDef.unlockCost <= 0 ? "Free" : $"{NumberFormatter.Format(subDef.unlockCost)} Ink";
                    sub.BuyButton.interactable = subDef.unlockCost <= 0 || Controller.Wallet.Balance >= subDef.unlockCost;
                }
            }
        }
    }
}
