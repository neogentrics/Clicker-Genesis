using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Main Menu's own info/Credits button - opens the real Credits panel directly (2026-08-07).
    /// Replaces the old ComingSoonButton stub, which kept showing "Credits is coming in a future
    /// update" even after real Credits content was built, since nothing had repointed this
    /// button. Polls for PauseMenuController.Instance in Update() instead of wiring in Awake()/
    /// Start(), since script execution order between this GameObject and the persistent
    /// PauseMenuController spawned in the same scene isn't guaranteed.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MainMenuCreditsButton : MonoBehaviour
    {
        private bool _wired;

        private void Update()
        {
            if (_wired || PauseMenuController.Instance == null) return;
            GetComponent<Button>().onClick.AddListener(PauseMenuController.Instance.ShowCreditsStandalone);
            _wired = true;
        }
    }
}
