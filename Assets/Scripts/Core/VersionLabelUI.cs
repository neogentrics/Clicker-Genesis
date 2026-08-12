using TMPro;
using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Small corner watermark on the Main Menu (2026-08-10, user's own idea - "so anyone running it
    /// will know which version they're on"). Reads Application.version (== ProjectSettings'
    /// bundleVersion baked into that build) once at launch - this is already correct "updates when
    /// the app updates" behavior for free: a Velopack-applied update swaps in a new binary with its
    /// own baked bundleVersion, so the very next launch reads the new value automatically. No link
    /// to AppUpdateManager needed.
    /// </summary>
    public class VersionLabelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label != null) label.text = $"v{Application.version} Beta";
        }
    }
}
