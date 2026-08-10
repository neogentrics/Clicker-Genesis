using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Drives the shared Prestige/Reset header buttons on PrestigeScreen.unity's still-active
    /// "Canvas" root, alongside the new SkillTreeV2 UI (2026-08-09, bug #88 real fix).
    ///
    /// Real root cause: those buttons live under Canvas/Header (active), but their onClick wiring
    /// and live-refresh logic lived on PrestigeScreenUI, which is attached to
    /// OldPrestigeTree_DEPRECATED - a root GameObject that's fully inactive (SetActive(false)) now
    /// that the SkillTreeV2 constellation UI replaced the old radial tree. An inactive GameObject's
    /// Awake()/OnStateChanged subscription never runs, so the buttons sat frozen at their
    /// last-edited placeholder text ("Prestige (Free)"/"Prestige") and clicking them did nothing -
    /// exactly the user's report ("I'm guessing they're linked to the old system").
    ///
    /// Fix: this small, focused component owns just the header's Prestige/Reset buttons - same
    /// logic as PrestigeScreenUI.Refresh()'s prestige-button section, copied rather than shared,
    /// since PrestigeScreenUI itself must stay disabled (it also drives the deprecated 105-node
    /// tree, which should not run). Attach to the active "Canvas" GameObject.
    /// </summary>
    public class SkillTreeHeaderPrestigeController : MonoBehaviour
    {
        public Button PrestigeButton;
        public TMP_Text PrestigeButtonLabel;
        public Button PrestigeResetButton;
        public TMP_Text PrestigeResetButtonLabel;
        public TMP_Text StatusLabel;

        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (PrestigeButton != null) PrestigeButton.onClick.AddListener(() => HandlePrestige(false));
            if (PrestigeResetButton != null) PrestigeResetButton.onClick.AddListener(() => HandlePrestige(true));
            if (Controller != null) Controller.OnStateChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void HandlePrestige(bool withReset)
        {
            Controller.PerformPrestige(withReset);
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Prestige == null) return;

            if (StatusLabel != null)
            {
                int totalPrestiges = Controller.Prestige.FreePrestigeCount + Controller.Prestige.ResetPrestigeCount;
                StatusLabel.text = totalPrestiges > 0
                    ? $"<b><color=#9B59B6>✦ You have prestiged before ({totalPrestiges}x)</color></b>"
                    : "";
            }

            var levels = Controller.Levels;
            if (PrestigeButton != null)
            {
                bool eligible = levels.IsPrestigeEligible;
                PrestigeButton.interactable = eligible;
                if (PrestigeButtonLabel != null)
                    PrestigeButtonLabel.text = eligible
                        ? $"Prestige (+{NumberFormatter.FormatWhole(Controller.PrestigeGracePreview)} Grace)"
                        : $"Prestige (Level {levels.PrestigeLevelThreshold} required)";
            }
            if (PrestigeResetButton != null)
            {
                bool eligible = levels.IsPrestigeEligible;
                PrestigeResetButton.interactable = eligible;
                if (PrestigeResetButtonLabel != null)
                    PrestigeResetButtonLabel.text = eligible
                        ? $"Reset for +{NumberFormatter.FormatWhole(Controller.PrestigeGracePreviewWithReset)} Grace"
                        : "Reset (Locked)";
            }
        }
    }
}
