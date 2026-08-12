using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// "Active Upgrades" panel on ClickerScreen (2026-08-06 rework, real user ask) - was a single
    /// multi-line TMP_Text with no way to explain any individual entry; now one row per owned
    /// skill node so each can carry its own hover tooltip (shown to the left, per the user's
    /// explicit request) describing what that specific upgrade actually does. Same row-list
    /// pattern as ScribeListUI/ManagerListUI/BookListUI, just simpler (no Button - rows are purely
    /// informational).
    /// </summary>
    public class PermanentUpgradesListUI : MonoBehaviour
    {
        private class Row
        {
            public GameObject GameObject;
        }

        public Transform Content;
        public GameObject RowTemplate;

        private readonly List<Row> rows = new List<Row>();
        private GameLoopController Controller => GameLoopController.Instance;

        /// <summary>Cheap change-detection so a purchase list that hasn't actually changed doesn't
        /// get destroyed and rebuilt every frame - OnStateChanged fires continuously from passive
        /// Ink ticking (the exact root cause bug #22 was fixed for elsewhere in this project;
        /// rebuilding a GameObject list on every tick would reintroduce that same class of lag).</summary>
        private int lastSignature = int.MinValue;

        private void Awake()
        {
            if (Controller != null) Controller.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Skills == null || Content == null || RowTemplate == null) return;

            var purchased = Controller.Skills.GetAllPurchased();

            int signature = purchased.Count;
            foreach (var (node, rank) in purchased)
                signature = signature * 31 + node.id.GetHashCode() ^ rank;
            if (signature == lastSignature) return;
            lastSignature = signature;

            foreach (var row in rows)
                if (row.GameObject != null) Destroy(row.GameObject);
            rows.Clear();

            if (purchased.Count == 0)
            {
                var emptyGo = Instantiate(RowTemplate, Content);
                emptyGo.SetActive(true);
                emptyGo.name = "Upgrade_Empty";
                var emptyLabel = emptyGo.GetComponent<TMP_Text>();
                if (emptyLabel != null) emptyLabel.text = "No permanent upgrades yet — spend Grace on the Skill Tree.";
                var emptyTooltip = emptyGo.GetComponent<HoverTooltip>();
                if (emptyTooltip != null) emptyTooltip.enabled = false;
                rows.Add(new Row { GameObject = emptyGo });
                UiRefreshUtil.ForceFullRefresh(Content);
                return;
            }

            foreach (var (node, rank) in purchased)
            {
                var rowGo = Instantiate(RowTemplate, Content);
                rowGo.SetActive(true);
                rowGo.name = $"Upgrade_{node.id}";

                var label = rowGo.GetComponent<TMP_Text>();
                if (label != null) label.text = $"{node.displayName} ({rank}/{node.maxRank})";

                var tooltip = rowGo.GetComponent<HoverTooltip>();
                if (tooltip == null) tooltip = rowGo.AddComponent<HoverTooltip>();
                tooltip.enabled = true;
                tooltip.PreferredDirection = HoverTooltip.Direction.Left;
                tooltip.Text = node.description;

                rows.Add(new Row { GameObject = rowGo });
            }

            UiRefreshUtil.ForceFullRefresh(Content);
        }
    }
}
