using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClickerGenesis.Save;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// The 3-slot save picker (2026-08-08, per CLAUDE.md's "Major standing authorization" spec) -
    /// reached from Main Menu's New Game or Continue button, which sets PendingEntryIsNewGame
    /// before navigating here. Same screen serves both entry points; only the per-slot button
    /// label/behavior and whether empty slots are clickable differ (see Refresh/OnSlotClicked).
    /// Reads save data directly off disk via SaveSlotManager - no GameLoopController instance is
    /// required to exist yet, since the player hasn't chosen which slot to load/create.
    /// </summary>
    public class SaveSlotScreenUI : MonoBehaviour
    {
        /// <summary>Set by SaveSlotEntryButton before this scene loads, consumed once in Awake.</summary>
        public static bool PendingEntryIsNewGame;

        [System.Serializable]
        public class SlotPanel
        {
            public GameObject Root;
            public TMP_Text TitleText;
            public TMP_Text SubText;
            public Button ActionButton;
            public TMP_Text ActionButtonLabel;
            public Button DeleteButton;
            public Button CopyButton;
        }

        public SlotPanel[] Slots = new SlotPanel[3];

        [Header("Delete confirmation")]
        public GameObject DeleteConfirmPanel;
        public TMP_Text DeleteConfirmMessage;
        public Button DeleteConfirmYesButton;
        public Button DeleteConfirmNoButton;

        [Header("Overwrite confirmation (New Game mode, slot already has a save)")]
        public GameObject OverwriteConfirmPanel;
        public TMP_Text OverwriteConfirmMessage;
        public Button OverwriteConfirmYesButton;
        public Button OverwriteConfirmNoButton;

        public TMP_Text StatusLabel;
        public TMP_Text ModeTitleLabel;
        public Button BackButton;

        private bool entryIsNewGame;
        private int pendingDeleteSlot = -1;
        private int pendingOverwriteSlot = -1;

        private void Awake()
        {
            entryIsNewGame = PendingEntryIsNewGame;
            PendingEntryIsNewGame = false;

            for (int i = 0; i < Slots.Length; i++)
            {
                int slot = i;
                if (Slots[i].ActionButton != null) Slots[i].ActionButton.onClick.AddListener(() => OnSlotClicked(slot));
                if (Slots[i].DeleteButton != null) Slots[i].DeleteButton.onClick.AddListener(() => RequestDelete(slot));
                if (Slots[i].CopyButton != null) Slots[i].CopyButton.onClick.AddListener(() => CopySlot(slot));
            }

            if (DeleteConfirmYesButton != null) DeleteConfirmYesButton.onClick.AddListener(ConfirmDelete);
            if (DeleteConfirmNoButton != null) DeleteConfirmNoButton.onClick.AddListener(() => DeleteConfirmPanel.SetActive(false));
            if (OverwriteConfirmYesButton != null) OverwriteConfirmYesButton.onClick.AddListener(ConfirmOverwrite);
            if (OverwriteConfirmNoButton != null) OverwriteConfirmNoButton.onClick.AddListener(() => OverwriteConfirmPanel.SetActive(false));
            if (BackButton != null) BackButton.onClick.AddListener(() => Navigate("MainMenu"));

            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(false);
            if (OverwriteConfirmPanel != null) OverwriteConfirmPanel.SetActive(false);
            if (ModeTitleLabel != null) ModeTitleLabel.text = entryIsNewGame ? "New Game — Choose a Slot" : "Continue — Choose a Slot";

            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                var summary = SaveSlotManager.GetSummary(i);
                var slot = Slots[i];
                if (slot.Root != null) slot.Root.SetActive(true);

                if (summary.HasSave)
                {
                    if (slot.TitleText != null) slot.TitleText.text = summary.ActiveBookDisplayName;
                    if (slot.SubText != null)
                    {
                        string bookProgress = summary.CurrentBookVerseTotal > 0
                            ? $"Chapter {summary.CurrentBookChapter}, Verse {summary.CurrentBookVerseNumber}\n" +
                              $"{summary.CurrentBookVersesOwned:N0}/{summary.CurrentBookVerseTotal:N0} verses ({summary.CurrentBookProgressPercent:F0}%)"
                            : "Not yet started";
                        slot.SubText.text =
                            $"Level {summary.Level}\n" +
                            $"{bookProgress}\n" +
                            $"{summary.BooksCompleted} of {summary.TotalBooks} OT books complete\n" +
                            $"Old Testament Progress: {summary.CompletionPercent:F0}%";
                    }
                    if (slot.ActionButtonLabel != null) slot.ActionButtonLabel.text = entryIsNewGame ? "Overwrite" : "Continue";
                    if (slot.ActionButton != null) slot.ActionButton.interactable = true;
                    if (slot.DeleteButton != null) slot.DeleteButton.gameObject.SetActive(true);
                    if (slot.CopyButton != null) slot.CopyButton.gameObject.SetActive(true);
                }
                else
                {
                    if (slot.TitleText != null) slot.TitleText.text = "Empty Slot";
                    if (slot.SubText != null) slot.SubText.text = entryIsNewGame ? "Tap to begin a new game" : "No save yet";
                    if (slot.ActionButtonLabel != null) slot.ActionButtonLabel.text = "New Game";
                    // Continue mode: empty slots aren't clickable ("only ever shows slots that
                    // already have a save" per spec) - New Game mode: any slot is fair game.
                    if (slot.ActionButton != null) slot.ActionButton.interactable = entryIsNewGame;
                    if (slot.DeleteButton != null) slot.DeleteButton.gameObject.SetActive(false);
                    if (slot.CopyButton != null) slot.CopyButton.gameObject.SetActive(false);
                }
            }
        }

        private void OnSlotClicked(int slot)
        {
            var summary = SaveSlotManager.GetSummary(slot);
            if (entryIsNewGame)
            {
                if (summary.HasSave)
                {
                    pendingOverwriteSlot = slot;
                    if (OverwriteConfirmMessage != null)
                        OverwriteConfirmMessage.text =
                            $"Overwrite the save in this slot ({summary.ActiveBookDisplayName}, Level {summary.Level})? This cannot be undone.";
                    if (OverwriteConfirmPanel != null) OverwriteConfirmPanel.SetActive(true);
                }
                else
                {
                    BeginNewGameSetup(slot);
                }
            }
            else
            {
                if (!summary.HasSave) return;
                // Real switch happens here, not via Awake()-consumed static fields - see
                // GameLoopController.SwitchToSlot's doc comment for why (it's a persistent
                // singleton whose Awake() only runs once, at the very first scene load).
                if (GameLoopController.Instance != null) GameLoopController.Instance.SwitchToSlot(slot, isNewGame: false);
                Navigate("ClickerScreen");
            }
        }

        private void ConfirmOverwrite()
        {
            if (OverwriteConfirmPanel != null) OverwriteConfirmPanel.SetActive(false);
            if (pendingOverwriteSlot >= 0) BeginNewGameSetup(pendingOverwriteSlot);
            pendingOverwriteSlot = -1;
        }

        private void BeginNewGameSetup(int slot)
        {
            SaveSlotManager.CurrentSlot = slot;
            NewGameSetupScreenUI.PendingSlotIndex = slot;
            Navigate("NewGameSetupScreen");
        }

        private void RequestDelete(int slot)
        {
            var summary = SaveSlotManager.GetSummary(slot);
            if (!summary.HasSave) return;
            pendingDeleteSlot = slot;
            if (DeleteConfirmMessage != null)
                DeleteConfirmMessage.text = $"Delete this save ({summary.ActiveBookDisplayName}, Level {summary.Level})? This cannot be undone.";
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(true);
        }

        private void ConfirmDelete()
        {
            if (DeleteConfirmPanel != null) DeleteConfirmPanel.SetActive(false);
            if (pendingDeleteSlot >= 0)
            {
                SaveSlotManager.DeleteSlot(pendingDeleteSlot);
                if (StatusLabel != null) StatusLabel.text = $"Slot {pendingDeleteSlot + 1} deleted.";
                Refresh();
            }
            pendingDeleteSlot = -1;
        }

        /// <summary>Copies into the next slot (wrapping), a deliberately simple "copy" affordance
        /// rather than a full destination picker - satisfies "each slot is copy-able" without a
        /// second picker UI. Revisit if the user wants explicit source/destination selection.</summary>
        private void CopySlot(int slot)
        {
            int dest = (slot + 1) % SaveSlotManager.SlotCount;
            SaveSlotManager.CopySlot(slot, dest);
            if (StatusLabel != null) StatusLabel.text = $"Copied Slot {slot + 1} to Slot {dest + 1}.";
            Refresh();
        }

        private void Navigate(string sceneName)
        {
            if (SceneTransitioner.Instance != null) SceneTransitioner.Instance.LoadScene(sceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
