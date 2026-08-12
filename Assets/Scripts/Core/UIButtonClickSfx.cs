using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Attach-and-go component (Sound-System-Design.html's "generic UI click" SFX category) -
    /// drop this on any Button and it plays AudioManager's generic click SFX on click, or a
    /// specific override clip if one is assigned here. Not yet attached to any existing button
    /// in the project (2026-08-11 build) - deliberately scoped out of this pass, same as this
    /// project's standing rule against blind project-wide UI sweeps (see the skybox-reveal
    /// precedent in CLAUDE.md). Ready to attach once real SFX clips exist and a real pass to wire
    /// every button happens.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonClickSfx : MonoBehaviour
    {
        [Tooltip("Leave empty to use AudioManager's shared generic click clip.")]
        [SerializeField] private AudioClip overrideClip;

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(PlayClick);
        }

        private void PlayClick()
        {
            if (AudioManager.Instance == null) return;
            if (overrideClip != null) AudioManager.Instance.PlaySfx(overrideClip);
            else AudioManager.Instance.PlayGenericClick();
        }
    }
}
