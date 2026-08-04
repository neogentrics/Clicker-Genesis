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

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Navigate);
        }

        private void Navigate()
        {
            if (string.IsNullOrEmpty(targetSceneName)) return;

            if (SceneTransitioner.Instance != null)
                SceneTransitioner.Instance.LoadScene(targetSceneName);
            else
                SceneManager.LoadScene(targetSceneName);
        }
    }
}
