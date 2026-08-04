using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Minimal OnGUI harness for manually testing the tap -> Ink -> buy-verse loop in Play mode.
    /// Not production UI — just enough to press Play and click through the loop.
    /// </summary>
    [RequireComponent(typeof(GameLoopController))]
    public class CoreLoopTestHUD : MonoBehaviour
    {
        private GameLoopController controller;

        private void Awake()
        {
            controller = GetComponent<GameLoopController>();
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 20;
            GUI.skin.button.fontSize = 20;

            GUILayout.BeginArea(new Rect(20, 20, 500, 500));

            GUILayout.Label($"Ink: {controller.Wallet.Balance:F1}");

            var levels = controller.Levels;
            GUILayout.Label($"Level {levels.CurrentLevel}  ({levels.XpIntoCurrentLevel}/{levels.XpRequiredForNextLevel} XP)");
            if (levels.IsPrestigeEligible)
            {
                GUILayout.Label("★ Prestige eligible!");
            }

            if (GUILayout.Button("Tap for Ink (+1)", GUILayout.Height(50)))
            {
                controller.TapForInk();
            }

            GUILayout.Space(20);

            if (controller.BookComplete)
            {
                GUILayout.Label("Book complete — no more verses to buy.");
            }
            else
            {
                GUILayout.Label($"Next verse cost: {controller.NextVerseCost:F1} Ink");
                if (GUILayout.Button("Buy Next Verse", GUILayout.Height(50)))
                {
                    controller.BuyNextVerse();
                }
            }

            GUILayout.Space(20);

            if (controller.HasUnlockedVerse)
            {
                var verse = controller.LastUnlockedVerse;
                GUILayout.Label($"Genesis {verse.Reference}", GUI.skin.box);
                GUILayout.Label(verse.Text, GUI.skin.box);
            }
            else
            {
                GUILayout.Label("No verse unlocked yet — buy the first one.");
            }

            GUILayout.EndArea();
        }
    }
}
