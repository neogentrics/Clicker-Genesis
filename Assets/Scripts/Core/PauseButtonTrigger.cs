using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>Attach to a Button to open the persistent PauseMenuController overlay. No
    /// serialized target needed (unlike SceneNavButton) - PauseMenuController is a runtime
    /// singleton spawned from MainMenu's GameRoot, so it doesn't exist as a scene object in
    /// ClickerScreen/BuyVerseScreen at edit time; resolved via Instance at click time instead.</summary>
    [RequireComponent(typeof(Button))]
    public class PauseButtonTrigger : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => PauseMenuController.Instance?.Show());
        }
    }
}
