using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Drives one ScrollRect's scrollSensitivity from GameSettings.ScrollSpeed (2026-08-09, bug
    /// #86) - drop this on every scrollable list in the game rather than hand-setting
    /// scrollSensitivity per-scene, so the one shared setting actually reaches all of them.
    /// Applies on enable and live if the setting changes while this object is active (e.g. the
    /// player adjusts it in Settings, then returns to a screen still in memory via a persistent
    /// canvas) - cheap, no per-frame cost, just an event subscription.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollSpeedApplier : MonoBehaviour
    {
        private ScrollRect scrollRect;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        private void OnEnable()
        {
            Apply();
            GameSettings.OnChanged += Apply;
        }

        private void OnDisable()
        {
            GameSettings.OnChanged -= Apply;
        }

        private void Apply()
        {
            if (scrollRect != null) scrollRect.scrollSensitivity = GameSettings.ScrollSpeed;
        }
    }
}
