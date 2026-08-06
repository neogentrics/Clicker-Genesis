using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Diagonal two-tone stripe shine sweeping across the XP bar fill, barbershop-pole style
    /// (2026-08-06 rework, user's explicit correction: the original was a single flat rectangular
    /// strip with square corners that didn't match the bar's rounded shape, and blended a
    /// translucent white overlay onto the green fill for a muddy three-color look instead of a
    /// clean two-tone one). Generates a small diagonal-stripe texture at runtime (two solid,
    /// opaque colors, no blending), tiles it across the Shine Image, and scrolls it sideways via
    /// uvRect - a real repeating scrolling pattern, no shader needed. Clipped to the fill's actual
    /// rounded silhouette via a Mask on Fill's own GameObject (using Fill's own sprite as the
    /// stencil), replacing the old RectMask2D, which only clips to a rectangle and ignored the
    /// sprite's rounded corners - that mismatch is exactly why the old shine read as square.
    /// </summary>
    public class XpBarShine : MonoBehaviour
    {
        [Tooltip("The XP bar's Fill RectTransform - Shine is resized every frame to exactly cover it.")]
        public RectTransform Fill;

        [Tooltip("The diagonal-stripe strip, child of Fill (clipped to Fill's rounded shape via the Mask now on Fill).")]
        public RectTransform Shine;

        [Tooltip("How fast the stripes scroll sideways (UV units/sec).")]
        public float ScrollSpeed = 0.6f;

        [Tooltip("Base tone - matches the bar's own fill color.")]
        public Color BaseColor = new Color(0f, 0.75f, 0.30f);

        [Tooltip("Highlight tone - the lighter stripe sweeping across.")]
        public Color HighlightColor = new Color(0.85f, 1f, 0.9f);

        [Tooltip("Texture size in pixels (also the diagonal repeat period). Smaller = more, thinner stripes.")]
        public int TextureSize = 32;

        [Tooltip("How many pixels wide the highlight band is within one period - kept small for 'thin lines, not blocks.'")]
        public int StripeWidth = 5;

        [Tooltip("How many stripe tiles fit across the bar's full width - higher = thinner, more numerous diagonal lines.")]
        public float TilesAcrossFill = 14f;

        // RawImage, not Image - Image's uvRect isn't exposed for scrolling a tiled sprite the way
        // this needs; RawImage's uvRect natively supports both the tile-repeat count (width/height
        // > 1, combined with the texture's Repeat wrap mode) and the scroll offset (x/y) in one
        // property, which is exactly this effect.
        private RawImage shineImage;
        private float uOffset;

        private void Awake()
        {
            if (Shine == null) return;

            var oldImage = Shine.GetComponent<Image>();
            if (oldImage != null) DestroyImmediate(oldImage);

            shineImage = Shine.GetComponent<RawImage>();
            if (shineImage == null) shineImage = Shine.gameObject.AddComponent<RawImage>();

            shineImage.texture = BuildStripeTexture();
            shineImage.color = Color.white; // no tint - the texture's own two colors show as authored

            // Left-pivoted, vertically stretched to Fill's height - Update() only ever needs to
            // set sizeDelta.x to make this exactly cover Fill's current width.
            Shine.anchorMin = new Vector2(0f, 0f);
            Shine.anchorMax = new Vector2(0f, 1f);
            Shine.pivot = new Vector2(0f, 0.5f);
            Shine.anchoredPosition = Vector2.zero;
        }

        private Texture2D BuildStripeTexture()
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int diagonal = (x + y) % TextureSize;
                    tex.SetPixel(x, y, diagonal < StripeWidth ? HighlightColor : BaseColor);
                }
            }
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (Fill == null || Shine == null || shineImage == null) return;

            float width = Fill.rect.width;
            if (width <= 1f)
            {
                if (Shine.gameObject.activeSelf) Shine.gameObject.SetActive(false);
                return;
            }
            if (!Shine.gameObject.activeSelf) Shine.gameObject.SetActive(true);

            Shine.sizeDelta = new Vector2(width, 0f);

            uOffset += ScrollSpeed * Time.deltaTime;
            shineImage.uvRect = new Rect(uOffset, 0f, TilesAcrossFill, 1f);
        }
    }
}
