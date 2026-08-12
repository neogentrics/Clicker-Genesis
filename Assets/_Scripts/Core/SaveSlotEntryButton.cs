using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>Main Menu's "New Game" / "Continue" buttons (2026-08-08, save-slot system) -
    /// both navigate to SaveSlotScreen, differing only in which mode that screen opens in
    /// (SaveSlotScreenUI.PendingEntryIsNewGame, consumed once in its Awake). Kept as a tiny
    /// dedicated component rather than reusing SceneNavButton, since SceneNavButton has no notion
    /// of "set this flag before navigating."</summary>
    [RequireComponent(typeof(Button))]
    public class SaveSlotEntryButton : MonoBehaviour
    {
        public bool IsNewGame;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Navigate);
        }

        private void Navigate()
        {
            SaveSlotScreenUI.PendingEntryIsNewGame = IsNewGame;
            if (SceneTransitioner.Instance != null) SceneTransitioner.Instance.LoadScene("SaveSlotScreen");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("SaveSlotScreen");
        }
    }
}
