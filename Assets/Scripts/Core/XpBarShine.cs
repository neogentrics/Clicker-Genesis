using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// A thin bright strip that sweeps left-to-right across the XP bar's fill, looping forever -
    /// the "pulsing to the right, like it's filling" effect the user wanted (2026-08-05). No
    /// custom shader/material needed: Shine is a child of Fill, so Fill's own RectTransform
    /// (which already shrinks to the current XP fraction via anchorMax.x) naturally clips the
    /// strip to the filled region - the script just walks Shine back and forth across Fill's
    /// current width every frame.
    /// </summary>
    public class XpBarShine : MonoBehaviour
    {
        [Tooltip("The XP bar's Fill RectTransform - its current width IS the sweep range.")]
        public RectTransform Fill;

        [Tooltip("Thin bright strip, child of Fill, stretched vertically.")]
        public RectTransform Shine;

        public float Speed = 220f;
        public float StripWidth = 50f;

        private float travelled;

        private void Update()
        {
            if (Fill == null || Shine == null) return;

            float width = Fill.rect.width;
            if (width <= 1f)
            {
                if (Shine.gameObject.activeSelf) Shine.gameObject.SetActive(false);
                return;
            }
            if (!Shine.gameObject.activeSelf) Shine.gameObject.SetActive(true);

            float travelRange = width + StripWidth;
            travelled += Speed * Time.deltaTime;
            if (travelled > travelRange) travelled -= travelRange;

            Shine.anchoredPosition = new Vector2(-StripWidth / 2f + travelled, Shine.anchoredPosition.y);
        }
    }
}
