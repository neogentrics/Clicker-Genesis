using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClickerGenesis.Data;
using ClickerGenesis.Save;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// New Game setup flow (2026-08-08, save-slot system) - reached only from SaveSlotScreen after
    /// picking an empty (or overwrite-confirmed) slot. Two steps: translation (KJV only for now,
    /// structured for more later per CLAUDE.md) then starting book (free pick among all 39 OT
    /// books, not gated by the normal mid-game "book N+1 needs book N finished" switching rule -
    /// this is the very first book, not a switch). Picking a book immediately starts the game.
    /// </summary>
    public class NewGameSetupScreenUI : MonoBehaviour
    {
        /// <summary>Set by SaveSlotScreenUI before this scene loads.</summary>
        public static int PendingSlotIndex;

        [Header("Step panels")]
        public GameObject TranslationStepPanel;
        public GameObject BookStepPanel;

        [Header("Translation step")]
        public Button NextButton;

        [Header("Book step")]
        public Transform BookListContent;
        public GameObject BookRowTemplate;
        public Button BackToTranslationButton;

        public Button BackButton;

        private void Awake()
        {
            if (NextButton != null) NextButton.onClick.AddListener(ShowBookStep);
            if (BackToTranslationButton != null) BackToTranslationButton.onClick.AddListener(ShowTranslationStep);
            if (BackButton != null) BackButton.onClick.AddListener(() => Navigate("SaveSlotScreen"));

            BuildBookRows();
            ShowTranslationStep();
        }

        private void ShowTranslationStep()
        {
            if (TranslationStepPanel != null) TranslationStepPanel.SetActive(true);
            if (BookStepPanel != null) BookStepPanel.SetActive(false);
        }

        private void ShowBookStep()
        {
            if (TranslationStepPanel != null) TranslationStepPanel.SetActive(false);
            if (BookStepPanel != null) BookStepPanel.SetActive(true);
        }

        private void BuildBookRows()
        {
            if (BookListContent == null || BookRowTemplate == null) return;

            foreach (var (resourceId, displayName) in CanonicalBookOrder.Books)
            {
                var rowGo = Instantiate(BookRowTemplate, BookListContent);
                rowGo.SetActive(true);
                rowGo.name = $"NewGameBookRow_{resourceId}";

                var nameText = rowGo.transform.Find("Reference")?.GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = displayName;
                var actionText = rowGo.transform.Find("Cost")?.GetComponent<TMP_Text>();
                if (actionText != null) actionText.text = "Select";

                string id = resourceId;
                var button = rowGo.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => BeginGame(id));
            }
        }

        private void BeginGame(string startingBookResourceId)
        {
            // Real switch happens here, not via Awake()-consumed static fields - see
            // GameLoopController.SwitchToSlot's doc comment for why (it's a persistent singleton
            // whose Awake() only runs once, at the very first scene load - it does NOT re-run just
            // because we're navigating to ClickerScreen again).
            if (GameLoopController.Instance != null)
                GameLoopController.Instance.SwitchToSlot(PendingSlotIndex, isNewGame: true, startingBookResourceId);
            Navigate("ClickerScreen");
        }

        private void Navigate(string sceneName)
        {
            if (SceneTransitioner.Instance != null) SceneTransitioner.Instance.LoadScene(sceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
