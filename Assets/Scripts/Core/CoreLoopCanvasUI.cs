using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Binds the confirmed art-direction Canvas UI (built via Editor tooling) to GameLoopController.
    /// Fields are assigned by the build script that constructs the hierarchy.
    /// </summary>
    public class CoreLoopCanvasUI : MonoBehaviour
    {
        public GameLoopController Controller;
        public Text InkLabel;
        public Text LevelLabel;
        public Text ReferenceLabel;
        public Text VerseText;
        public Text StatusLabel;
        public Button TapButton;
        public Button BuyButton;

        /// <summary>
        /// Assigns the controller reference at build time (in the Editor, before Play mode).
        /// Does NOT wire button listeners or the OnStateChanged subscription here — those are
        /// plain C# delegate state that does not survive Unity's domain reload on entering Play
        /// mode, so they're (re-)established in Awake()/Start() instead, which re-run every time
        /// Play mode actually starts.
        /// </summary>
        public void Initialize(GameLoopController controller)
        {
            Controller = controller;
        }

        private void Awake()
        {
            if (Controller == null) return;
            TapButton.onClick.AddListener(HandleTap);
            BuyButton.onClick.AddListener(HandleBuy);
            Controller.OnStateChanged += Refresh;
            // Refresh() here too, not just Start() — Start() was observed to never fire in this
            // project's headless Unity-MCP automation for freshly-loaded scenes. Awake is
            // guaranteed either way, so calling it here is strictly more robust.
            Refresh();
        }

        private void HandleTap() => Controller.TapForInk();
        private void HandleBuy() => Controller.BuyNextVerse();

        private void Refresh()
        {
            // Controller.Awake() only runs in Play mode — if this UI was wired up in the Editor
            // (not playing), Wallet/Levels/Verses may still be null. The static placeholder text
            // set when the UI was built already matches the correct initial state, so it's safe
            // to just bail here and wait for the first real OnStateChanged in Play mode.
            if (Controller.Wallet == null || Controller.Levels == null) return;

            InkLabel.text = $"Ink: {Controller.Wallet.Balance:F1}";

            var levels = Controller.Levels;
            string prestigeMark = levels.IsPrestigeEligible ? "  ★ Prestige eligible" : "";
            LevelLabel.text = $"Level {levels.CurrentLevel} ({levels.XpIntoCurrentLevel}/{levels.XpRequiredForNextLevel} XP){prestigeMark}";

            if (Controller.HasUnlockedVerse)
            {
                var verse = Controller.LastUnlockedVerse;
                ReferenceLabel.text = $"{Controller.Verses.BookName} {verse.Reference}";
                VerseText.text = verse.Text;
            }
            else
            {
                ReferenceLabel.text = "No verse unlocked yet";
                VerseText.text = "Buy the first verse to reveal it here.";
            }

            if (Controller.BookComplete)
            {
                StatusLabel.text = "Book complete!";
                BuyButton.interactable = false;
            }
            else
            {
                StatusLabel.text = $"Next verse cost: {Controller.NextVerseCost:F1} Ink";
                BuyButton.interactable = true;
            }
        }

        private void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= Refresh;
        }
    }
}
