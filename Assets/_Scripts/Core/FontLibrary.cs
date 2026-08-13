using UnityEngine;
using TMPro;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real font-selection feature (2026-08-13, user's explicit ask - accessibility/readability,
    /// "if the player doesn't like the font I'm currently using, or it's hard for them to read
    /// depending on their age, they can pick a different one"). Every entry here is a genuinely
    /// open-license font already sitting in the project (no new downloads needed): Ibarra Real Nova
    /// (current default, SIL OFL), Jost (SIL OFL, bundled in the Modern GDR icon pack), Roboto
    /// (Apache 2.0), Liberation Sans (SIL OFL, TMP's own bundled default). Each entry optionally
    /// carries a true-bold TMP_FontAsset; when one isn't available (Roboto/Liberation Sans have no
    /// bold face in this project), FontApplier falls back to TMP's own algorithmic bold via the
    /// FontStyles.Bold flag - same behavior every screen already relied on before real bold weights
    /// existed at all.
    /// </summary>
    [CreateAssetMenu(fileName = "FontLibrary", menuName = "Clicker Genesis/Font Library")]
    public class FontLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string DisplayName;
            public TMP_FontAsset Regular;
            public TMP_FontAsset Bold; // optional - null means "use Regular + algorithmic bold"
        }

        public Entry[] Entries;
    }
}
