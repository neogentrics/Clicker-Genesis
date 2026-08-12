using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real celebration moment for an achievement actually unlocking (2026-08-12), not just a
    /// color change - plays SuperPixelEffectsGigapack's round_light_burst_001_large_yellow frame
    /// sequence at the unlocking card's own position (or a fallback anchor if that card isn't
    /// currently visible under the active scope/filter). Same coroutine-driven
    /// frame-swap-then-destroy pattern as AmbientSparkleSpawner, which is already proven working
    /// live in this project - just triggered on-demand instead of on a random idle timer, and at
    /// gold/large scale instead of ambient/small.
    /// </summary>
    public class AchievementUnlockBurst : MonoBehaviour
    {
        public Sprite[] Frames;
        public float FrameDuration = 0.045f;
        public float SpriteSize = 320f;

        public void PlayAt(RectTransform parent, Vector2 anchoredPosition)
        {
            if (Frames == null || Frames.Length == 0 || parent == null) return;

            var go = new GameObject("AchievementUnlockBurst", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(SpriteSize, SpriteSize);
            rt.anchoredPosition = anchoredPosition;
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = Frames[0];
            img.raycastTarget = false;
            img.preserveAspect = true;

            StartCoroutine(PlayAndDestroy(go, img));
        }

        private IEnumerator PlayAndDestroy(GameObject go, Image img)
        {
            for (int i = 0; i < Frames.Length; i++)
            {
                if (img == null) yield break;
                img.sprite = Frames[i];
                yield return new WaitForSeconds(FrameDuration);
            }
            if (go != null) Destroy(go);
        }
    }
}
