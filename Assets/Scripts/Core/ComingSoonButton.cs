using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// A real, wired-up button for a feature that isn't built yet. Clicking it shows a toast
    /// message on the shared status label instead of navigating anywhere — used so unfinished
    /// features (Prestige, Credits) are visibly present and honest about their state, rather
    /// than silently missing from the menu.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ComingSoonButton : MonoBehaviour
    {
        public TMP_Text StatusLabel;
        public string FeatureName = "This feature";

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(ShowComingSoon);
        }

        private void ShowComingSoon()
        {
            if (StatusLabel != null)
                StatusLabel.text = $"{FeatureName} is coming in a future update.";
        }
    }
}
