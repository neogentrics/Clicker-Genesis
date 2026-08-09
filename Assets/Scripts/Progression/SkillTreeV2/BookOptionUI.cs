using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// One row in the Convergence book-selection panel (Rule 4). Deliberately its own small
    /// component rather than folded into SkillTreeUIManager - "don't build the entire UI in one
    /// script" applies to the book menu rows just as much as to the tree itself.
    /// </summary>
    public class BookOptionUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button button;

        private BookMasteryData book;
        private SkillTreeUIManager manager;

        public void Initialize(BookMasteryData bookData, SkillTreeUIManager owner)
        {
            book = bookData;
            manager = owner;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => manager.HandleBookOptionClicked(book));
            }
        }

        public void Refresh(SkillTreeRuntimeState runtime)
        {
            if (book == null || runtime == null) return;

            if (nameText != null) nameText.text = book.displayName;

            bool unlocked = runtime.IsBookUnlocked(book.bookResourceId);
            double cost = runtime.NextBookUnlockCost;

            if (statusText != null)
                statusText.text = unlocked ? "Enter Mastery" : $"[Cost {cost:0} Grace]";

            if (button != null)
                button.interactable = unlocked || runtime.Grace >= cost;
        }
    }
}
