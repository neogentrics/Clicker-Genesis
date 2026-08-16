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
                var def = scribes.GetDefinition(i);
                // Was def.HasManager - generalized 2026-08-08 to also cover Law tiers (a
                // teaching/ritual with a purpose instead of a person), which have neither
                // managerName nor managerUnlockLevel set and would otherwise silently never get a
                // row at all. See ScribeDefinition.HasManagerRow's doc comment.
                if (!def.HasManagerRow) continue;

                int tierIndex = i;
                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.SetActive(true);
                rowGo.name = def.tierType == ClickerGenesis.Economy.TierType.Law
                    ? $"LawRow_{def.displayName}"
                    : $"ManagerRow_{def.managerName}";

                var row = new Row
                {
                    TierIndex = tierIndex,
                    Icon = rowGo.transform.Find("Icon").GetComponent<Image>(),
                    NameText = rowGo.transform.Find("Name").GetComponent<TMP_Text>(),
                    DescText = rowGo.transform.Find("Desc").GetComponent<TMP_Text>(),
                    CostText = rowGo.transform.Find("BuyButton/CostText").GetComponent<TMP_Text>(),
                    BuyButton = rowGo.transform.Find("BuyButton").GetComponent<Button>()
                };
                EnableCostTextAutoSize(row.CostText);
                // Icon now comes straight from data (2026-08-08, replaces the old per-book
                // hardcoded C# switch) - ScribeDefinition.managerIcon is set per-tier in each
                // book's ScribeSetConfig asset, so a new book's roster never requires editing this
                // file. Deliberately a separate field from the scribe tier's own icon - a
                // manager's symbolic icon often differs from their tier's object icon.
                row.Icon.sprite = def.managerIcon;
                row.Icon.color = row.Icon.sprite != null ? Color.white : new Color(0, 0, 0, 0);
                row.BuyButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPurchaseClick();
                    Controller.BuyManager(tierIndex);
                });
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

        /// <summary>Converts a verse index (within the active book) into a human "BookName X:Y"
        /// reference (2026-08-06, generalized 2026-08-08 for multi-book - was hardcoded to
        /// "Genesis" before Exodus existed) - falls back to a plain index if the active book's
        /// VerseDatabase isn't loaded yet for any reason.</summary>
        private string DescribeVerse(int verseIndex)
        {
            var db = Controller.Verses;
            if (db == null || !db.HasVerse(verseIndex)) return $"verse {verseIndex + 1}";
            var v = db.GetVerse(verseIndex);
            return $"{db.BookName} {v.Reference}";
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

            // See ScribeListUI.Refresh's identical check for why (2026-08-08, multi-book economy).
            if (Controller.ActiveBookResourceId != builtForBookId)
            {
                RebuildRows();
                ForceLayoutRebuild();
                return;
            }

            var scribes = Controller.Scribes;
            int level = Controller.EffectiveManagerLevel;
            // Overseer's Wisdom (Grace Skill Tree) boosts every manager's output bonus - the row
            // must show the real effective total, not just the manager's own base percentage, or
            // the number here silently drifts from what GetTierInkPerSecond actually pays out.
            double skillBonusBoost = Controller.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ManagerBonusBoost);

            foreach (var row in rows)
            {
                var def = scribes.GetDefinition(row.TierIndex);

                // Law tier (2026-08-08): a teaching/ritual, not a person - shown once reachable
                // with no separate Ink cost or level gate (informational, not a second purchase
                // step, per CharacterIndex-Integration-Mapping.md §8's explicit design call).
                if (def.tierType == ClickerGenesis.Economy.TierType.Law)
                {
                    row.NameText.text = def.displayName;
                    // Deliberately NOT scribes.IsUnlocked() here (2026-08-16 bug fix) - that method
                    // has a tier-0 "always true" bootstrap exception meant for the Scribes tab's
                    // production row (so the player can always afford their first verse), which has
                    // nothing to do with a Law tier's own informational content. A book whose tier 0
                    // happens to be Law-typed (Deuteronomy's "Unworn Sandal", 2 Samuel's "The Song of
                    // the Bow") was showing its full purpose text as already "reachable" on a brand
                    // new save with zero verses bought, purely because it inherited the scribe
                    // bootstrap rule. Law-tier reachability must always be a real verse-index check.
                    bool reachable = Controller.NextVerseIndex > def.unlockAtVerseIndex;

                    if (reachable)
                    {
                        string deliveredByLine = "";
                        if (!string.IsNullOrEmpty(def.deliveredByManagerId))
                        {
                            string deliveredByName = scribes.GetManagerDisplayName(def.deliveredByManagerId);
                            if (!string.IsNullOrEmpty(deliveredByName))
                                deliveredByLine = $"\n<i>as taught by {deliveredByName}</i>";
                        }
                        row.DescText.text = def.purpose + deliveredByLine;
                        // Law tiers have no Ink purchase step - "—" here means "nothing to buy,"
                        // not "locked," so it's only correct once the tier is actually reachable.
                        row.CostText.text = "—";
                    }
                    else
                    {
                        // 2026-08-09, real user report: an unreached Law tier was showing the same
                        // "—" as a reachable one, reading as available when it wasn't - must match
                        // every other row type's "Locked" convention while genuinely unreachable.
                        row.DescText.text = ReqLine(false, "", $"Reach {DescribeVerse(def.unlockAtVerseIndex)}");
                        row.CostText.text = "Locked";
                    }

                    row.BuyButton.interactable = false;
                    continue;
                }

                row.NameText.text = def.managerName;

                bool active = scribes.IsManagerActive(row.TierIndex, level);
                bool unlocked = scribes.IsManagerUnlocked(row.TierIndex);
                bool levelEligible = scribes.IsManagerLevelEligible(row.TierIndex, level);
                // A manager tied to a scribe tier the player hasn't unlocked yet (verse progress)
                // must stay locked even if the level threshold is already met - fixes a real bug
                // where Noah could be bought before Ark's Manifest itself was unlocked. Gates
                // against the ACTIVE book's own progress (2026-08-08) - correct now that Scribes
                // always mirrors whichever book is active (multi-book economy).
                bool scribeTierUnlocked = scribes.IsUnlocked(row.TierIndex, Controller.NextVerseIndex);
                // The manager's own character verse (real CharacterIndex data, 2026-08-06) -
                // deliberately separate from scribeTierUnlocked above, see ScribeSystem's doc
                // comment on IsManagerVerseReached for why they can't share a field.
                bool managerVerseReached = scribes.IsManagerVerseReached(row.TierIndex, Controller.NextVerseIndex);
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
                    // 2026-08-09, real user report: this line only ever showed the manager's own
                    // base managerBonusMultiplier, silently ignoring any owned "loyalty-boost"
                    // submanager on the same tier - e.g. a manager giving 25% base + 5% from a
                    // hired Support submanager was still only showing "+25%" here, when the real
                    // total paid out by GetTierInkPerSecond was 30%. Show the real total.
                    double loyaltyBoost = scribes.GetLoyaltyBoost(row.TierIndex);
                    double totalBonus = def.managerBonusMultiplier + loyaltyBoost;
                    string desc = $"+{totalBonus * 100:F0}% {def.displayName} output";
                    if (loyaltyBoost > 0)
                        desc += $"\n<color=#2E7D46>includes +{loyaltyBoost * 100:F0}% from Support</color>";
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
                            $"{DescribeVerse(def.managerUnlockAtVerseIndex)} reached",
                            $"Reach {DescribeVerse(def.managerUnlockAtVerseIndex)}"),
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
                else if (!allRequirementsMet)
                {
                    // 2026-08-09, real user report (Deuteronomy's Managers tab): the button was
                    // showing a real Ink price even while requirement lines above it were still
                    // red/unmet - reading as "buyable right now" when it wasn't. The button must
                    // never show a price until every requirement is actually satisfied.
                    row.CostText.text = "Locked";
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
