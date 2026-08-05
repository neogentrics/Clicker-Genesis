using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>The Clicker screen: tap-for-Ink only. Buying verses lives on BuyVerseScreen.</summary>
    public class ClickerScreenUI : MonoBehaviour
    {
        public TMP_Text InkLabel;
        public TMP_Text ClickingPowerLabel;

        [Header("XP bar (LevelLabel is the text drawn inside the bar)")]
        public TMP_Text LevelLabel;
        public Image XpBarFill;

        public Button TapButton;
        public TMP_Text TapButtonLabel;

        [Header("Tap feedback (visual life)")]
        public RectTransform TapButtonRect;
        public Transform FloatingTextParent;
        public GameObject FloatingTextTemplate;

        [Header("Click Power upgrade (bulk-buy quantity shares the Scribes tab's Multiplier control)")]
        public Button ClickPowerButton;
        public TMP_Text ClickPowerButtonLabel;
        public TMP_Text TapValueLabel;

        [Header("Prestige (locked until eligible; the reset-for-bonus-Grace flow lives on the future Prestige screen, not here)")]
        public Button PrestigeButton;
        public TMP_Text PrestigeButtonLabel;
        public TMP_Text PrestigeTooltipLabel;
        public TMP_Text GraceLabel;
        public GameObject StatusBanner;
        public TMP_Text StatusLabel;

        [Header("Scribes/Managers tabs (2026-08-04)")]
        public Button ScribesTabButton;
        public Button ManagersTabButton;
        public GameObject ScribeListRoot;
        public GameObject ManagerListRoot;
        public Image ScribesTabBackground;
        public Image ManagersTabBackground;

        [Header("Scribes header (2026-08-04) - 'Multiplier' caption + bulk-buy cycle button, Scribes tab only")]
        public TMP_Text ScribesHeaderLabel;
        public GameObject ScribeMultiplierButtonRoot;

        [Header("Manager auto-buy controls (2026-08-04) - visible on both Scribes/Managers tabs, next to Multiplier")]
        public Button AutoBuyToggleButton;
        public TMP_Text AutoBuyToggleButtonLabel;
        public Button AutoBuyReserveButton;
        public TMP_Text AutoBuyReserveButtonLabel;

        private static readonly Color ActiveTabColor = new Color(0.957f, 0.925f, 0.847f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.72f, 0.65f, 0.53f, 1f);

        private GameLoopController Controller => GameLoopController.Instance;

        private void Awake()
        {
            // If GameRoot was never spawned (Play started from a scene other than MainMenu),
            // bail out to MainMenu instead of running with a null Controller. See
            // GameLoopController.EnsureBootstrapped() for why.
            if (!GameLoopController.EnsureBootstrapped()) return;

            // Wiring lives in Awake(), not Start() — Start() was observed to never fire in this
            // project's headless Unity-MCP automation for objects in a freshly-loaded scene
            // (Awake runs synchronously as part of the scene load itself; Start needs a later
            // frame tick that reliably never came between separate automated tool calls). Awake
            // is guaranteed either way, so this is strictly more robust, not just a workaround.
            TapButton.onClick.AddListener(HandleTap);
            if (ClickPowerButton != null) ClickPowerButton.onClick.AddListener(HandleBuyClickPower);
            if (PrestigeButton != null) PrestigeButton.onClick.AddListener(HandlePrestigeClick);
            if (ScribesTabButton != null) ScribesTabButton.onClick.AddListener(() => SetTab(false));
            if (ManagersTabButton != null) ManagersTabButton.onClick.AddListener(() => SetTab(true));
            if (AutoBuyToggleButton != null) AutoBuyToggleButton.onClick.AddListener(() => Controller?.ToggleManagerAutoBuy());
            if (AutoBuyReserveButton != null) AutoBuyReserveButton.onClick.AddListener(() => Controller?.CycleManagerAutoBuyReserve());
            if (Controller != null) Controller.OnStateChanged += Refresh;
            SetTab(false);
            Refresh();
        }

        /// <summary>Same pattern as BuyVerseScreenUI.SetTab for Verses/Chapters.</summary>
        private void SetTab(bool managers)
        {
            if (ScribeListRoot != null) ScribeListRoot.SetActive(!managers);
            if (ManagerListRoot != null) ManagerListRoot.SetActive(managers);
            if (ScribesTabBackground != null) ScribesTabBackground.color = managers ? InactiveTabColor : ActiveTabColor;
            if (ManagersTabBackground != null) ManagersTabBackground.color = managers ? ActiveTabColor : InactiveTabColor;

            // The bulk-buy cycle button only makes sense for Scribes (Managers has no bulk-buy of
            // its own), but the header caption itself stays populated on both tabs ("Managers"
            // instead of a blank string) so the header area doesn't read as a big empty gap -
            // previously this was a fixed "Scribes" title that never updated at all.
            if (ScribesHeaderLabel != null) ScribesHeaderLabel.text = managers ? "Managers" : "Multiplier";
            if (ScribeMultiplierButtonRoot != null) ScribeMultiplierButtonRoot.SetActive(!managers);
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }

        private void HandleTap()
        {
            Controller?.TapForInk();
            if (TapButtonRect != null) StartCoroutine(PulseTapButton());
            if (FloatingTextTemplate != null) SpawnFloatingText();
        }

        private IEnumerator PulseTapButton()
        {
            const float duration = 0.12f;
            Vector3 baseScale = Vector3.one;
            Vector3 peakScale = Vector3.one * 1.1f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                TapButtonRect.localScale = Vector3.Lerp(baseScale, peakScale, t / duration);
                yield return null;
            }
            t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                TapButtonRect.localScale = Vector3.Lerp(peakScale, baseScale, t / duration);
                yield return null;
            }
            TapButtonRect.localScale = baseScale;
        }

        private void HandleBuyClickPower() => Controller?.BuyClickPowerBulk();

        private void SpawnFloatingText()
        {
            var go = Instantiate(FloatingTextTemplate, FloatingTextParent);
            go.SetActive(true);
            var text = go.GetComponent<TMP_Text>();
            text.text = $"+{NumberFormatter.Format(Controller.EffectiveTapAmount)} Ink";
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(Random.Range(-40f, 40f), 0f);
            StartCoroutine(AnimateFloatingText(rt, text));
        }

        private IEnumerator AnimateFloatingText(RectTransform rt, TMP_Text text)
        {
            const float duration = 0.6f;
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + new Vector2(0f, 120f);
            Color startColor = text.color;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duration;
                rt.anchoredPosition = Vector2.Lerp(start, end, p);
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - p);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        // Opens the Grace skill tree / Prestige screen instead of instantly prestiging - the
        // actual prestige action (free or reset) now lives there, per the user's explicit pivot:
        // "there is supposed to pull up a secondary screen so that they can spend their grace on
        // something" (2026-08-04).
        private void HandlePrestigeClick() => UnityEngine.SceneManagement.SceneManager.LoadScene("PrestigeScreen");

        // The status message needs its own backing banner (StatusBanner) to read as a message
        // rather than bare text floating on the stone backdrop - this keeps banner visibility in
        // sync with the text instead of just setting StatusLabel.text directly everywhere.
        private void SetStatus(string message)
        {
            if (StatusLabel != null) StatusLabel.text = message;
            if (StatusBanner != null) StatusBanner.SetActive(!string.IsNullOrEmpty(message));
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Wallet == null || Controller.Levels == null) return;

            InkLabel.text = $"Ink: {NumberFormatter.Format(Controller.Wallet.Balance)}";
            if (ClickingPowerLabel != null && Controller.Scribes != null)
                ClickingPowerLabel.text = $"Clicking Power\n{NumberFormatter.Format(Controller.EffectiveInkPerSecond)} Ink/s";

            var levels = Controller.Levels;
            LevelLabel.text = $"Level {levels.CurrentLevel} — {levels.XpIntoCurrentLevel}/{levels.XpRequiredForNextLevel} XP";
            if (XpBarFill != null)
            {
                float fraction = levels.XpRequiredForNextLevel > 0
                    ? (float)levels.XpIntoCurrentLevel / levels.XpRequiredForNextLevel
                    : 0f;
                // Sliced (not Filled) so the rounded-pill sprite's corners render correctly at any
                // width - Filled crops the raw sprite rect and can't preserve a 9-slice border,
                // which made the fill's rounded shape mismatch the Background's actual bounds.
                var fillRt = XpBarFill.rectTransform;
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(fraction, 1f);
            }

            if (TapValueLabel != null)
                TapValueLabel.text = $"Each tap: {NumberFormatter.Format(Controller.EffectiveTapAmount)} Ink";

            // Just the current tap power - no "-> preview" arrow. Updates automatically whenever
            // EffectiveTapAmount changes (i.e. the player buys a Click Power upgrade), since this
            // whole method re-runs on every OnStateChanged tick.
            if (TapButtonLabel != null)
                TapButtonLabel.text = $"Tap: {NumberFormatter.Format(Controller.EffectiveTapAmount)}";

            if (ClickPowerButton != null)
            {
                double bulkCost = Controller.ClickPowerBulkCost;
                ClickPowerButton.interactable = Controller.Wallet.Balance >= bulkCost;
                if (ClickPowerButtonLabel != null)
                    ClickPowerButtonLabel.text = $"{NumberFormatter.Format(bulkCost)} Ink";
            }

            if (AutoBuyToggleButtonLabel != null)
                AutoBuyToggleButtonLabel.text = Controller.ManagerAutoBuyEnabled ? "Auto-Buy: On" : "Auto-Buy: Off";
            if (AutoBuyReserveButtonLabel != null)
                AutoBuyReserveButtonLabel.text = Controller.ManagerAutoBuyReserve <= 0
                    ? "Reserve: None"
                    : $"Reserve: {NumberFormatter.FormatWhole(Controller.ManagerAutoBuyReserve)}";

            if (GraceLabel != null && Controller.Prestige != null)
                GraceLabel.text = $"Grace: {NumberFormatter.FormatWhole(Controller.Prestige.Grace)}";

            if (PrestigeButton != null)
            {
                // Progressive disclosure for a first-time player (hidden entirely until near-
                // eligible - a far-off feature is clutter, a near one is a goal), but once the
                // player has EVER prestiged, this button is their only way back to the Grace skill
                // tree screen to keep spending Grace - it must never disappear again just because
                // a free prestige reset their level back to 1 (2026-08-05, real bug: player had
                // Grace but the button vanished and they couldn't reach the tree at all).
                bool near = levels.IsPrestigeNear || Controller.Prestige.PrestigeCount > 0;
                PrestigeButton.gameObject.SetActive(near);
                if (near)
                {
                    // Always clickable once visible - it just opens the Grace skill tree screen
                    // (not an instant prestige action), where nodes already bought with past Grace
                    // stay spendable/viewable even before the next prestige is ready, and the
                    // actual Prestige (free) / Reset (bonus Grace) choice lives on that screen.
                    // Labeled "Skill Tree", not "Prestige" (2026-08-05, explicit user correction:
                    // calling this button "Prestige" was misleading since it doesn't itself
                    // prestige anything - it just opens the screen where that choice is made).
                    bool eligible = levels.IsPrestigeEligible;
                    PrestigeButton.interactable = true;
                    if (PrestigeButtonLabel != null) PrestigeButtonLabel.text = "Skill Tree";
                    if (PrestigeTooltipLabel != null)
                        PrestigeTooltipLabel.text = eligible
                            ? $"Ready! +{NumberFormatter.FormatWhole(Controller.PrestigeGracePreview)} Grace"
                            : $"View skill tree — prestige unlocks at Level {levels.PrestigeLevelThreshold}";
                }
            }
        }
    }
}
