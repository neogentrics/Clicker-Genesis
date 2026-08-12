using UnityEngine;
using UnityEngine.UI;
using ClickerGenesis.Save;

namespace ClickerGenesis.Core
{
    /// <summary>Main Menu Play button (2026-08-10, user's explicit redesign ask). Replaces the old
    /// always-both-visible New Game / Continue buttons - which let a player start a "new" game even
    /// when no save existed to continue from, since nothing on the Main Menu itself ever checked
    /// save state - with a single green Play button that opens a popup offering both choices. The
    /// popup's Continue Game option is only enabled when at least one of the 3 save slots actually
    /// has a save; New Game is always available.
    ///
    /// The popup's two buttons are the same GameObjects the old Main Menu used
    /// (<see cref="SaveSlotEntryButton"/> already handles "set which mode, then navigate to
    /// SaveSlotScreen") - this component only decides whether Continue is interactable before the
    /// popup opens, it doesn't duplicate any navigation logic.</summary>
    [RequireComponent(typeof(Button))]
    public class MainMenuPlayController : MonoBehaviour
    {
        public GameObject PopupPanel;
        public Button ContinueGameButton;
        public Image ContinueGameImage;
        public Button ClosePopupButton;
        public Button BackdropButton;

        private static readonly Color ContinueEnabledColor = new Color(0.45f, 0.65f, 1.0f, 1f);
        private static readonly Color ContinueDisabledColor = new Color(0.42f, 0.42f, 0.42f, 1f);

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OpenPopup);
            if (ClosePopupButton != null) ClosePopupButton.onClick.AddListener(ClosePopup);
            if (BackdropButton != null) BackdropButton.onClick.AddListener(ClosePopup);
            if (PopupPanel != null) PopupPanel.SetActive(false);
        }

        private void OpenPopup()
        {
            bool hasAnySave = false;
            for (int i = 0; i < SaveSlotManager.SlotCount; i++)
            {
                if (SaveSlotManager.HasSave(i)) { hasAnySave = true; break; }
            }

            if (ContinueGameButton != null) ContinueGameButton.interactable = hasAnySave;
            if (ContinueGameImage != null) ContinueGameImage.color = hasAnySave ? ContinueEnabledColor : ContinueDisabledColor;

            if (PopupPanel != null) PopupPanel.SetActive(true);
        }

        private void ClosePopup()
        {
            if (PopupPanel != null) PopupPanel.SetActive(false);
        }
    }
}
