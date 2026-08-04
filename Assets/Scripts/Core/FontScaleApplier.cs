using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Attach once to a screen's root Canvas. Captures each TMP_Text's authored font size the
    /// first time it's seen, then scales it by GameSettings.FontScale - lets the Settings
    /// font-size option affect every screen without touching each UI script individually.
    ///
    /// Self-healing by design: re-applies on every GameLoopController.OnStateChanged tick (which
    /// already fires constantly - taps, passive income, purchases), not just once in Awake. This
    /// is what picks up text created after Awake (e.g. ScribeListUI's dynamically-built rows)
    /// without needing this component to know about every other script that creates text at
    /// runtime, or depending on Awake/Start ordering between sibling components.
    /// </summary>
    public class FontScaleApplier : MonoBehaviour
    {
        private readonly Dictionary<TMP_Text, float> baseSizes = new Dictionary<TMP_Text, float>();

        private void Awake()
        {
            ApplyToAll();
            GameSettings.OnChanged += ApplyToAll;
            if (GameLoopController.Instance != null)
                GameLoopController.Instance.OnStateChanged += ApplyToAll;
        }

        private void OnDestroy()
        {
            GameSettings.OnChanged -= ApplyToAll;
            if (GameLoopController.Instance != null)
                GameLoopController.Instance.OnStateChanged -= ApplyToAll;
        }

        private void ApplyToAll()
        {
            float scale = GameSettings.FontScale;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;
                if (!baseSizes.TryGetValue(text, out float baseSize))
                {
                    // First time seen: whatever size it currently has IS the authored baseline,
                    // since nothing has scaled it yet.
                    baseSize = text.fontSize;
                    baseSizes[text] = baseSize;
                }
                text.fontSize = baseSize * scale;
            }
        }
    }
}
