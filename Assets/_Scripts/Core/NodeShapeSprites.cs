using System.Collections.Generic;
using UnityEngine;
using ClickerGenesis.Progression;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Procedurally generates a background sprite per SkillNodeShape (2026-08-08 Skill Tree
    /// redesign) - no new art assets needed (project has no diamond/hexagon/star UI frame sprites,
    /// and per the standing "no AI-generated art" rule, these are simple rasterized polygons, not
    /// commissioned/generated art). Generated once per shape and cached - same pattern as the XP
    /// bar's procedurally generated shine texture.
    /// </summary>
    public static class NodeShapeSprites
    {
        private const int Size = 128;
        private static readonly Dictionary<SkillNodeShape, Sprite> cache = new Dictionary<SkillNodeShape, Sprite>();

        public static Sprite Get(SkillNodeShape shape)
        {
            if (cache.TryGetValue(shape, out var existing) && existing != null) return existing;
            var sprite = Generate(shape);
            cache[shape] = sprite;
            return sprite;
        }

        private static Sprite Generate(SkillNodeShape shape)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            Vector2 center = new Vector2(Size / 2f, Size / 2f);
            float radius = Size / 2f - 2f;
            List<Vector2> poly = shape == SkillNodeShape.Circle ? null : BuildPolygon(shape, center, radius);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = poly == null
                        ? Vector2.Distance(p, center) <= radius
                        : PointInPolygon(p, poly);
                    tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }

        private static List<Vector2> BuildPolygon(SkillNodeShape shape, Vector2 center, float radius)
        {
            if (shape == SkillNodeShape.Star) return BuildStar(center, radius);

            int sides;
            float startAngleDeg = -90f;
            switch (shape)
            {
                case SkillNodeShape.Diamond: sides = 4; break;
                case SkillNodeShape.Hexagon: sides = 6; break;
                case SkillNodeShape.Triangle: sides = 3; break;
                default: sides = 32; break; // unreachable (Circle short-circuits above)
            }

            var pts = new List<Vector2>(sides);
            for (int i = 0; i < sides; i++)
            {
                float angle = (startAngleDeg + i * 360f / sides) * Mathf.Deg2Rad;
                pts.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return pts;
        }

        private static List<Vector2> BuildStar(Vector2 center, float radius)
        {
            var pts = new List<Vector2>(10);
            float inner = radius * 0.45f;
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? radius : inner;
                float angle = (-90f + i * 36f) * Mathf.Deg2Rad;
                pts.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
            }
            return pts;
        }

        private static bool PointInPolygon(Vector2 p, List<Vector2> poly)
        {
            bool inside = false;
            int j = poly.Count - 1;
            for (int i = 0; i < poly.Count; i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
                j = i;
            }
            return inside;
        }
    }
}
