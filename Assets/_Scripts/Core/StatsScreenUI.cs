using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real dedicated Stats scene (2026-08-10, Phase 6 of the v2 achievement/UI redesign) -
    /// replaces the old Pause Menu popup panel, which was outgrowing a small overlay footprint as
    /// more content got added to it (bonus breakdown, etc.). Builds one bordered panel per stat
    /// section (Ink/Grace/Progress/Current Output/Active Bonuses/Store) inside a scrolling list,
    /// per the user's real mid-build correction - "different sections... in different panels...
    /// you can scroll through them" - matching how every other list in this project already
    /// presents content as individual cards rather than one text blob. Reuses
    /// PauseMenuController.BuildStatsSections exactly - the actual stats content/formatting isn't
    /// duplicated here, just laid out differently.
    /// </summary>
    public class StatsScreenUI : MonoBehaviour
    {
        public Transform Content;
        public GameObject SectionPanelTemplate;
        public Button BackButton;

        private readonly List<(TMP_Text Title, TMP_Text Body)> panels = new List<(TMP_Text, TMP_Text)>();

        private void Awake()
        {
            if (!GameLoopController.EnsureBootstrapped()) return;

            if (BackButton != null) BackButton.onClick.AddListener(GoBack);
            BuildPanels();
            Refresh();

            var controller = GameLoopController.Instance;
            if (controller != null) controller.OnStateChanged += Refresh;
        }

        private void OnDestroy()
        {
            var controller = GameLoopController.Instance;
            if (controller != null) controller.OnStateChanged -= Refresh;
        }

        /// <summary>Panels are built once against a fixed 6-section shape (Ink/Grace/Progress/
        /// Current Output/Active Bonuses/Store never changes count at runtime) - Refresh() only
        /// updates existing text, never Instantiates/Destroys, same no-per-frame-rebuild
        /// discipline as every other list in this project.</summary>
        private void BuildPanels()
        {
            if (Content == null || SectionPanelTemplate == null) return;

            foreach (var (title, _) in PauseMenuController.BuildStatsSections(GameLoopController.Instance))
            {
                var panelGo = Instantiate(SectionPanelTemplate, Content);
                panelGo.SetActive(true);
                panelGo.name = "StatsSection_" + title;

                var titleTmp = panelGo.transform.Find("Title")?.GetComponent<TMP_Text>();
                var bodyTmp = panelGo.transform.Find("Body")?.GetComponent<TMP_Text>();
                panels.Add((titleTmp, bodyTmp));
            }
        }

        private void Refresh()
        {
            var sections = PauseMenuController.BuildStatsSections(GameLoopController.Instance);
            for (int i = 0; i < panels.Count && i < sections.Length; i++)
            {
                if (panels[i].Title != null) panels[i].Title.text = sections[i].Title;
                if (panels[i].Body != null) panels[i].Body.text = sections[i].Body;
            }
        }

        private void GoBack()
        {
            string target = PauseMenuController.Instance != null ? PauseMenuController.Instance.ConsumeStatsReturnScene() : "ClickerScreen";
            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }
    }
}
