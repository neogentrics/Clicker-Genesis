using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Main Menu 2.5D pass (2026-08-08): periodically spawns a small fading light-burst sprite
    /// animation at a random position within this RectTransform, using the already-owned
    /// SuperPixelEffectsGigapack (round_light_burst_001_small_yellow, 9 frames). Meant to sit
    /// behind the parchment panels, in the starfield margin revealed by shrinking the Background -
    /// purely atmospheric, not interactive (raycastTarget off).
    /// </summary>
    public class AmbientSparkleSpawner : MonoBehaviour
    {
        public Sprite[] Frames;
        public RectTransform[] SpawnAreas;
        public float MinInterval = 1.5f;
        public float MaxInterval = 3.5f;
        public float FrameDuration = 0.06f;
        public float SpriteSize = 90f;
        public int MaxConcurrent = 4;

        private readonly List<GameObject> active = new List<GameObject>();

        private void OnEnable()
        {
            if (Frames != null && Frames.Length > 0 && SpawnAreas != null && SpawnAreas.Length > 0)
                StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(MinInterval, MaxInterval));
                active.RemoveAll(go => go == null);
                if (active.Count >= MaxConcurrent) continue;
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            var spawnArea = SpawnAreas[Random.Range(0, SpawnAreas.Length)];
            var go = new GameObject("AmbientSparkle", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(spawnArea, false);
            rt.sizeDelta = new Vector2(SpriteSize, SpriteSize);

            var bounds = spawnArea.rect;
            float halfW = Mathf.Min(SpriteSize * 0.5f, bounds.width * 0.5f);
            float halfH = Mathf.Min(SpriteSize * 0.5f, bounds.height * 0.5f);
            float x = Random.Range(bounds.xMin + halfW, bounds.xMax - halfW);
            float y = Random.Range(bounds.yMin + halfH, bounds.yMax - halfH);
            rt.anchoredPosition = new Vector2(x, y);

            var img = go.GetComponent<Image>();
            img.sprite = Frames[0];
            img.raycastTarget = false;
            img.preserveAspect = true;

            active.Add(go);
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
