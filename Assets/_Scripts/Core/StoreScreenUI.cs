using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real Store scene (2026-08-12) so the Pause Menu's Store button goes somewhere instead of
    /// staying permanently disabled - "Coming Soon" content only, since the actual Talents/IAP
    /// structure (see Store-Monetization.html in the offline doc folder) is a deliberately separate,
    /// later design pass. Same standalone-scene pattern as Stats/Credits.
    /// </summary>
    public class StoreScreenUI : MonoBehaviour
    {
        public TMP_Text Body;
        public Button BackButton;

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (BackButton != null) BackButton.onClick.AddListener(GoBack);
            if (Body != null)
                Body.text = "The Store isn't open yet.\n\n" +
                    "Talents (the game's optional support currency) and everything it can buy are " +
                    "still being designed - nothing here will ever be required to unlock content, " +
                    "keep gameplay progress, or finish reading a single verse of Scripture.\n\n" +
                    "Check back in a future update.";
        }

        private void GoBack()
        {
            string target = PauseMenuController.Instance != null ? PauseMenuController.Instance.ConsumeStoreReturnScene() : "MainMenu";
            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }
    }
}
