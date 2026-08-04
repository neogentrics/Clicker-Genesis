using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Persistent singleton (spawned once from Main Menu's GameRoot, survives scene loads) that
    /// shows/hides a full-screen pause overlay from anywhere in gameplay. Replaces the two
    /// separate corner icon buttons (Settings gear + Menu hamburger) that used to live on every
    /// gameplay screen - one Pause button now opens this instead, with Settings/Main Menu/Store
    /// as options inside it (2026-08-04, explicit user request instead of continuing to cram
    /// more corner buttons onto individual screens).
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Overlay root (starts inactive)")]
        public GameObject OverlayRoot;

        [Header("Buttons")]
        public Button ResumeButton;
        public Button SettingsButton;
        public Button MainMenuButton;
        public Button StoreButton;
        public TMP_Text StoreButtonLabel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            // Same reasoning as every other screen's Awake-not-Start wiring in this project - see
            // ClickerScreenUI.Awake for the full explanation.
            if (ResumeButton != null) ResumeButton.onClick.AddListener(Hide);
            if (SettingsButton != null) SettingsButton.onClick.AddListener(OpenSettings);
            if (MainMenuButton != null) MainMenuButton.onClick.AddListener(GoToMainMenu);
            if (StoreButton != null)
            {
                StoreButton.interactable = false;
                if (StoreButtonLabel != null) StoreButtonLabel.text = "Store (Coming Soon)";
            }

            if (OverlayRoot != null) OverlayRoot.SetActive(false);
        }

        public void Show()
        {
            if (OverlayRoot != null) OverlayRoot.SetActive(true);
        }

        public void Hide()
        {
            if (OverlayRoot != null) OverlayRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (OverlayRoot == null) return;
            if (OverlayRoot.activeSelf) Hide();
            else Show();
        }

        private void OpenSettings()
        {
            Hide();
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.RecordSettingsReturnScene(SceneManager.GetActiveScene().name);
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene("SettingsScreen");
            else
                SceneManager.LoadScene("SettingsScreen");
        }

        private void GoToMainMenu()
        {
            Hide();
            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        }
    }
}
