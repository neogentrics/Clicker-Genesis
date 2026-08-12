using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real "you opened something grand" entrance (2026-08-12) - a soft scale+fade unfurl on the
    /// main panel instead of the previous instant cut. Deliberately Update()-driven (per-frame
    /// Lerp against Time.unscaledDeltaTime) rather than a WaitForSeconds coroutine - an earlier
    /// coroutine-driven fade attempt on this project reliably stalled forever under this project's
    /// headless Unity-MCP test automation (coroutines/yields don't reliably tick between separate
    /// automated tool calls), so this avoids that whole class of trap. Unscaled time so it isn't
    /// affected by any future pause/slow-mo feature.
    /// </summary>
    public class ScreenEntranceAnimator : MonoBehaviour
    {
        public CanvasGroup Group;
        public RectTransform Target;
        public float Duration = 0.45f;
        public float StartScale = 0.85f;

        private float elapsed;
        private bool playing;

        private void OnEnable()
        {
            elapsed = 0f;
            playing = Group != null && Target != null;
            if (playing)
            {
                Group.alpha = 0f;
                Target.localScale = Vector3.one * StartScale;
            }
        }

        private void Update()
        {
            if (!playing) return;

            elapsed += Time.unscaledDeltaTime;
            float t = Duration > 0f ? Mathf.Clamp01(elapsed / Duration) : 1f;
            float eased = 1f - (1f - t) * (1f - t); // ease-out quad - fast start, gentle settle

            Group.alpha = eased;
            Target.localScale = Vector3.one * Mathf.Lerp(StartScale, 1f, eased);

            if (t >= 1f)
            {
                Group.alpha = 1f;
                Target.localScale = Vector3.one;
                playing = false;
            }
        }
    }
}
