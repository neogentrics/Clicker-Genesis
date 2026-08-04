using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real, working "quit the app" button for desktop builds (2026-08-04) - there was previously
    /// no way to close the game from the Main Menu at all besides Alt+F4/closing the window.
    /// Mobile has no equivalent convention (players expect the OS home/back gesture instead), so
    /// this hides itself entirely there rather than showing a button that does something unusual.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
        private void Awake()
        {
            if (!GameSettings.IsResolutionSelectionSupported)
            {
                gameObject.SetActive(false);
                return;
            }
            GetComponent<Button>().onClick.AddListener(() => Application.Quit());
        }
    }
}
