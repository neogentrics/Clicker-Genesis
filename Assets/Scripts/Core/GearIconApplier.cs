using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>Applies the procedural gear glyph (GearIconSprite) to this GameObject's own Image
    /// at Awake - a tiny reusable component so any Pause/Settings icon in the project can swap to
    /// the readable procedural gear without each screen needing custom wiring code.</summary>
    [RequireComponent(typeof(Image))]
    public class GearIconApplier : MonoBehaviour
    {
        private void Awake()
        {
            var img = GetComponent<Image>();
            img.sprite = GearIconSprite.Get();
            img.type = Image.Type.Simple;
        }
    }
}
