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

        [Header("Click Power upgrade (bulk buy: 1x/2x/3x/4x/5x = 1/5/10/20/100)")]
        public Button ClickPowerButton;
        public TMP_Text ClickPowerButtonLabel;
        public TMP_Text TapValueLabel;
        public Button ClickPowerMultiplierButton;
        public TMP_Text ClickPowerMultiplierButtonLabel;

        [Header("Prestige (locked until eligible)")]
        public Button PrestigeButton;
        public TMP_Text PrestigeButtonLabel;
        public TMP_Text StatusLabel;

        [Header("Scribes/Managers tabs (2026-08-04)")]
        public Button ScribesTabButton;
        public Button ManagersTabButton;
        public GameObject ScribeListRoot;
        public GameObject ManagerListRoot;
        public Image ScribesTabBackground;
        public Image ManagersTabBackground;

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
            if (ClickPowerMultiplierButton != null) ClickPowerMultiplierButton.onClick.AddListener(() => Controller?.CycleClickPowerBuyMultiplier());
            if (PrestigeButton != null) PrestigeButton.onClick.AddListener(HandlePrestigeClick);
            if (ScribesTabButton != null) ScribesTabButton.onClick.AddListener(() => SetTab(false));
            if (ManagersTabButton != null) ManagersTabButton.onClick.AddListener(() => SetTab(true));
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

        /// <summary>See BuyVerseScreenUI.MultiplierTierLabel - same 1/5/10/20/... quantity -> "Nx"
        /// tier label mapping, extended with a 5x tier since Click Power's cap is 100 not 20.</summary>
        private static string MultiplierTierLabel(int quantity) => quantity switch
        {
            1 => "1x",
            5 => "2x",
            10 => "3x",
            20 => "4x",
            100 => "5x",
            _ => $"{quantity}x"
        };

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

        private void HandlePrestigeClick()
        {
            if (StatusLabel != null)
                StatusLabel.text = "Prestige is coming in a future update.";
        }

        private void Refresh()
        {
            if (Controller == null || Controller.Wallet == null || Controller.Levels == null) return;

            InkLabel.text = $"Ink: {NumberFormatter.Format(Controller.Wallet.Balance)}";
            if (ClickingPowerLabel != null && Controller.Scribes != null)
                ClickingPowerLabel.text = $"Clicking Power: {NumberFormatter.Format(Controller.Scribes.TotalInkPerSecond(Controller.Levels.CurrentLevel))} Ink/s";

            var levels = Controller.Levels;
            LevelLabel.text = $"Level {levels.CurrentLevel} — {levels.XpIntoCurrentLevel}/{levels.XpRequiredForNextLevel} XP";
            if (XpBarFill != null)
                XpBarFill.fillAmount = levels.XpRequiredForNextLevel > 0
                    ? (float)levels.XpIntoCurrentLevel / levels.XpRequiredForNextLevel
                    : 0f;

            if (TapValueLabel != null)
                TapValueLabel.text = $"Each tap: {NumberFormatter.Format(Controller.EffectiveTapAmount)} Ink";

            // The tap-value preview lives on the Tap circle itself; the Upgrade button shows only
            // the cost, per explicit request (was previously reversed / both crammed together).
            if (TapButtonLabel != null)
                TapButtonLabel.text =
                    $"Tap: {NumberFormatter.Format(Controller.EffectiveTapAmount)} -> {NumberFormatter.Format(Controller.ClickPowerBulkPreviewTapAmount)}";

            if (ClickPowerButton != null)
            {
                double bulkCost = Controller.ClickPowerBulkCost;
                ClickPowerButton.interactable = Controller.Wallet.Balance >= bulkCost;
                if (ClickPowerButtonLabel != null)
                    ClickPowerButtonLabel.text = $"{NumberFormatter.Format(bulkCost)} Ink";
            }

            if (ClickPowerMultiplierButton != null)
            {
                // Only appears once the player has bought at least 5 upgrades - before that, a
                // 5/10/20/100 batch option is more clutter than choice.
                bool showMultiplier = Controller.ClickPowerLevel >= 5;
                ClickPowerMultiplierButton.gameObject.SetActive(showMultiplier);
                if (showMultiplier && ClickPowerMultiplierButtonLabel != null)
                    ClickPowerMultiplierButtonLabel.text = MultiplierTierLabel(Controller.ClickPowerBuyMultiplier);
            }

            if (PrestigeButton != null)
            {
                // Progressive disclosure: hidden entirely until near-eligible, not shown-but-
                // locked from the very start — a far-off feature is clutter, a near one is a goal.
                bool near = levels.IsPrestigeNear;
                PrestigeButton.gameObject.SetActive(near);
                if (near)
                {
                    bool eligible = levels.IsPrestigeEligible;
                    PrestigeButton.interactable = eligible;
                    if (PrestigeButtonLabel != null)
                        PrestigeButtonLabel.text = eligible ? "Prestige" : "Prestige (Locked)";
                }
            }
        }
    }
}
