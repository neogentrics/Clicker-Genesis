using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Slow color-cycling ambient glow (2026-08-12), built for reward-moment screens like
    /// Achievements - cycles a soft radial-gradient Image through the same jewel-tone family used
    /// by the card shimmer materials (Blue/Green/Purple/Orange), so the whole screen's background
    /// feels quietly alive rather than a flat static color, without competing with card content.
    /// </summary>
    public class JewelGlowCycler : MonoBehaviour
    {
        public Image Target;
        public float CycleDuration = 6f;
        [Range(0f, 1f)] public float MaxAlpha = 0.35f;
        [Range(0f, 1f)] public float MinAlpha = 0.12f;
        public float PulseSpeed = 0.6f;

        private static readonly Color[] JewelTones =
        {
            new Color(0.35f, 0.55f, 1f),   // blue
            new Color(0.4f, 0.85f, 0.55f), // green
            new Color(0.75f, 0.45f, 1f),   // purple
            new Color(1f, 0.65f, 0.3f),    // orange
        };

        private float t;

        private void Update()
        {
            if (Target == null) return;

            t += Time.deltaTime;
            float cyclePos = (t / CycleDuration) % JewelTones.Length;
            int idx = Mathf.FloorToInt(cyclePos);
            float frac = cyclePos - idx;
            Color from = JewelTones[idx];
            Color to = JewelTones[(idx + 1) % JewelTones.Length];
            Color baseColor = Color.Lerp(from, to, frac);

            float pulse = Mathf.Lerp(MinAlpha, MaxAlpha, (Mathf.Sin(t * PulseSpeed) + 1f) * 0.5f);
            baseColor.a = pulse;
            Target.color = baseColor;
        }
    }
}
