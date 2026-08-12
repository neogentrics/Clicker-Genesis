using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Procedurally generates a plain gear glyph (2026-08-09) - replaces MetallicUIKit's
    /// miniIconSettings.png on the Skill Tree's Pause button, which turned out to be a low-contrast
    /// embossed banner/shield shape at small icon size, not a readable gear ("this looks weird",
    /// real user report). Same rasterized-polygon technique as NodeShapeSprites - a plain white
    /// silhouette on a transparent background so it tints like every other icon in this project
    /// (via Image.color) instead of carrying its own baked background/color the way every
    /// MetallicUIKit "miniIcon*" sprite does.
    /// </summary>
    public static class GearIconSprite
    {
        private const int Size = 128;
        private static Sprite cached;

        public static Sprite Get()
        {
            if (cached != null) return cached;
            cached = Generate();
            return cached;
        }

        private static Sprite Generate()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            Vector2 center = new Vector2(Size / 2f, Size / 2f);
            float outerRadius = Size / 2f - 4f;
            float ringOuter = outerRadius * 0.68f;
            float ringInner = ringOuter * 0.55f;
            float holeRadius = outerRadius * 0.28f;
            const int teethCount = 8;
            float stepAngle = Mathf.PI * 2f / teethCount;
            float toothHalfAngle = stepAngle * 0.30f;
            float toothDepth = ringOuter * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float dist = d.magnitude;
                    float angle = Mathf.Atan2(d.y, d.x);
                    if (angle < 0f) angle += Mathf.PI * 2f;

                    bool inRing = dist >= ringInner && dist <= ringOuter;

                    bool inTooth = false;
                    if (dist > ringOuter && dist <= ringOuter + toothDepth)
                    {
                        float nearestToothCenter = Mathf.Round(angle / stepAngle) * stepAngle;
                        float delta = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, nearestToothCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                        if (delta <= toothHalfAngle) inTooth = true;
                    }

                    bool inHole = dist <= holeRadius;
                    bool solid = (inRing || inTooth) && !inHole;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, solid ? 1f : 0f));
                }
            }
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
