using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>Generic "go to this scene" button — attach to any Button to wire navigation.</summary>
    [RequireComponent(typeof(Button))]
    public class SceneNavButton : MonoBehaviour
    {
        public string targetSceneName;

        /// <summary>Magic value for a Settings screen's Back button - resolves at click time to
        /// wherever the player actually came from (see SceneTransitioner.LastSettingsReturnScene),
        /// instead of a hardcoded scene name.</summary>
        public const string SettingsReturnTarget = "__SettingsReturn__";

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Navigate);
        }

        private void Navigate()
        {
            string target = targetSceneName;
            if (string.IsNullOrEmpty(target)) return;

            if (target == "SettingsScreen" && SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.RecordSettingsReturnScene(SceneManager.GetActiveScene().name);
            else if (target == SettingsReturnTarget)
                target = SceneTransitioner.Instance != null ? SceneTransitioner.Instance.LastSettingsReturnScene : "MainMenu";

            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene(target);
            else
                SceneManager.LoadScene(target);
        }
    }
}
