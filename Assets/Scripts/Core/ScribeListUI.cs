using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Builds one row per scribe tier from GameLoopController.Instance.Scribes and keeps them
    /// refreshed. Rows are built once in Awake (tier count is static per book); Refresh() only
    /// updates text/interactable state, it doesn't rebuild the hierarchy.
    /// </summary>
    public class ScribeListUI : MonoBehaviour
    {
        private class Row
        {
            public Image Icon;
            public TMP_Text NameText;
            public TMP_Text DescText;
            public TMP_Text CostText;
            public TMP_Text OwnedText;
            public Button BuyButton;
        }

        public Transform Content;
        public GameObject RowTemplate;
        public Sprite ScrollIcon;
        public Sprite JournalIcon;

        [Header("Bulk buy (bug #26 - Scribes never had one like Verses/Click Power did)")]
        public Button MultiplierButton;
        public TMP_Text MultiplierButtonLabel;

        private readonly List<Row> rows = new List<Row>();
        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (Controller != null) Controller.OnStateChanged += Refresh;
            if (MultiplierButton != null) MultiplierButton.onClick.AddListener(() => Controller?.CycleScribeBuyMultiplier());
            BuildRows();
            Refresh();
            ForceLayoutRebuild();
            // The immediate refresh above was verified correct in scripted testing but reported
            // still blank in real, uninterrupted Play sessions - see UiRefreshUtil.DeferredFullRefresh.
            StartCoroutine(UiRefreshUtil.DeferredFullRefresh(Content));
        }

        /// <summary>
        /// Runtime-instantiated rows under a VerticalLayoutGroup/ContentSizeFitter don't always
        /// get an automatic layout pass the same frame they're added (observed here: every row
        /// stayed stuck at its raw default RectTransform — 100x100 at the origin — until this was
        /// forced). Without this, all rows silently stack on top of each other at (0,0) and the
        /// list reads as empty even though every row and its Buy button genuinely exist.
        ///
        /// Separately, this VerticalLayoutGroup's own width calculation was observed to produce a
        /// width nearly 2x the actual viewport (deterministically, not a one-frame timing fluke —
        /// reproduced identically across repeated rebuilds), for reasons not fully pinned down.
        /// Rather than depend on a component miscomputing width, height stacking is left to the
        /// group but width is forced directly via a full-stretch anchor per row immediately after,
        /// which is immune to whatever the group's horizontal math is doing.
        ///
        /// IMPORTANT: this must only touch DIRECT children of Content (the rows themselves), not
        /// recurse into their descendants - an earlier version used GetComponentsInChildren and
        /// force-stretched every nested element (Icon, Name, BuyButton, etc.) to full row width,
        /// turning a small 70x70 icon into a giant solid-colored block covering the whole row and
        /// obscuring the name text underneath it. Direct-children-only avoids that entirely.
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

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void BuildRows()
        {
            if (Controller == null || Controller.Scribes == null) return;

            var scribes = Controller.Scribes;
            for (int i = 0; i < scribes.TierCount; i++)
            {
                int tierIndex = i;
                var def = scribes.GetDefinition(i);

                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.SetActive(true);
                rowGo.name = $"Row_{def.id}";

                var row = new Row
                {
                    Icon = rowGo.transform.Find("Icon").GetComponent<Image>(),
                    NameText = rowGo.transform.Find("Name").GetComponent<TMP_Text>(),
                    DescText = rowGo.transform.Find("Desc").GetComponent<TMP_Text>(),
                    CostText = rowGo.transform.Find("BuyButton/CostText").GetComponent<TMP_Text>(),
                    OwnedText = rowGo.transform.Find("Owned").GetComponent<TMP_Text>(),
                    BuyButton = rowGo.transform.Find("BuyButton").GetComponent<Button>()
                };

                row.NameText.text = def.displayName;
                row.Icon.sprite = tierIndex == 1 ? ScrollIcon : (tierIndex == 4 ? JournalIcon : null);
                row.Icon.color = row.Icon.sprite != null
                    ? Color.white
                    : Color.Lerp(Hex("#7A5C2E"), Hex("#D4AF37"), tierIndex / (float)(scribes.TierCount - 1));

                row.BuyButton.onClick.AddListener(() => Controller.BuyScribeBulk(tierIndex));

                rows.Add(row);
            }
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Scribes == null) return;

            var scribes = Controller.Scribes;
            int level = Controller.Levels.CurrentLevel;

            if (MultiplierButtonLabel != null)
                MultiplierButtonLabel.text = Controller.ScribeBuyMultiplier == GameLoopController.MaxBuyMultiplier
                    ? "Max"
                    : $"x{Controller.ScribeBuyMultiplier}";

            for (int i = 0; i < rows.Count; i++)
            {
                var def = scribes.GetDefinition(i);
                var row = rows[i];
                // Genesis-specific gating (not the active book's cursor) - see
                // GameLoopController.GenesisNextVerseIndex for why (Phase F book-switching, 2026-08-06).
                bool unlocked = scribes.IsUnlocked(i, Controller.GenesisNextVerseIndex);

                if (!unlocked)
                {
                    // No "[Locked]" suffix here - the Buy button itself already says "Locked",
                    // repeating it on the name too is redundant (unlike verse/chapter rows, whose
                    // buttons show an actual price, not the literal word "Locked").
                    row.NameText.text = def.displayName;
                    row.DescText.text = $"Unlocks after buying verse {def.unlockAtVerseIndex + 1}.";
                    row.OwnedText.text = "";
                    row.CostText.text = "Locked";
                    row.BuyButton.interactable = false;
                    continue;
                }

                int owned = scribes.GetOwned(i);
                double cost = Controller.ScribeBulkCost(i);
                bool managerActive = scribes.IsManagerActive(i, level);
                float milestoneMultiplier = scribes.GetOwnedMilestoneMultiplier(i);

                row.NameText.text = def.displayName;

                string desc = def.flavorText;
                if (milestoneMultiplier > 1f)
                    // No "(N owned)" - the milestone tier itself is the meaningful information;
                    // the raw owned count is already shown separately on the Owned line below.
                    desc += $"\n<b><color=#D4372A>×{milestoneMultiplier:F1} milestone bonus!</color></b>";
                // Grace Skill Tree's Scribe's Diligence branch boosts milestone bonuses globally
                // (folded into EffectiveInkPerSecond's skillIncomeBoost, not this tier's own curve
                // value) - surfaced here too so the row doesn't read as if the skill does nothing
                // (2026-08-06, real user report: "that should show here on this screen also").
                double scribeMilestoneBoost = Controller.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ScribeMilestoneBoost);
                if (scribeMilestoneBoost > 0)
                    desc += $"\n<color=#2E7D46>+{scribeMilestoneBoost * 100:F0}% from Scribe's Diligence</color>";
                // Manager line only appears once actually active - no "takes over at level N"
                // spoiler beforehand, and simplified wording (no % breakdown) once it is active.
                if (managerActive)
                    desc += $"\n<color=#2E7D46>{def.managerName} is managing this scribe.</color>";
                row.DescText.text = desc;

                double managerBonusBoost = Controller.Skills.GetTotalEffect(ClickerGenesis.Progression.SkillEffectType.ManagerBonusBoost);
                row.OwnedText.text = $"Owned: {owned}  ·  {NumberFormatter.Format(scribes.GetTierInkPerSecond(i, level, Controller.ProgressMultiplier, managerBonusBoost))} Ink/s";
                string quantityPrefix = Controller.ScribeBuyMultiplier == GameLoopController.MaxBuyMultiplier
                    ? "Max  "
                    : Controller.ScribeBuyMultiplier > 1 ? $"x{Controller.ScribeBuyMultiplier}  " : "";
                row.CostText.text = $"{quantityPrefix}{NumberFormatter.Format(cost)} Ink";
                row.BuyButton.interactable = Controller.Wallet.Balance >= cost;
            }

            // NOT calling UiRefreshUtil.ForceFullRefresh here on purpose - Refresh() runs every
            // single frame (GameLoopController.Update ticks passive Ink and fires OnStateChanged
            // continuously), and ForceFullRefresh's GetComponentsInChildren + SetAllDirty +
            // ForceMeshUpdate + LayoutRebuilder.ForceRebuildLayoutImmediate over every row is
            // expensive - doing that 60x/second was the real cause of the game feeling laggy
            // (bug #22), not scene-load time itself. Plain TMP_Text.text assignment above already
            // dirties and repaints correctly in real Play; ForceFullRefresh is still used once in
            // Awake/ForceLayoutRebuild for the one-time post-Instantiate staleness issue.
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
