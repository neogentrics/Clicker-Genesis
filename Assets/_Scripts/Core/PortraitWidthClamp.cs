using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// SettingsScreen's main Panel is a fixed-width box (1740 canvas units) tuned for a wide
    /// desktop window - CanvasScaler's Scale-With-Screen-Size only applies a uniform SCALE factor,
    /// it never touches a RectTransform's own sizeDelta, so a fixed-width element stays exactly
    /// 1740 units wide regardless of the live reference resolution. On a 1080-wide portrait canvas
    /// that overflows both edges by ~330 units each side, taking every row's Label (anchored near
    /// the panel's own left edge) off-screen with it, while the Minus/Plus buttons (anchored
    /// nearer the row's right side) happen to still land in view - which is why the settings rows
    /// showed buttons with no labels at all. Fix: switch to a full-stretch anchor with a small
    /// margin in portrait, restoring the original fixed width in landscape so desktop is unchanged.
    /// </summary>
    public class PortraitWidthClamp : MonoBehaviour
    {
        public RectTransform Target;
        public float PortraitMargin = 24f;

        private Vector2 originalAnchorMin, originalAnchorMax, originalAnchoredPos, originalSizeDelta;
        private bool capturedOriginal;

        private int lastWidth = -1;
        private int lastHeight = -1;
        private bool lastIsPortrait;

        private void Awake()
        {
            CaptureOriginal();
            Apply();
        }

        private void CaptureOriginal()
        {
            if (capturedOriginal || Target == null) return;
            originalAnchorMin = Target.anchorMin;
            originalAnchorMax = Target.anchorMax;
            originalAnchoredPos = Target.anchoredPosition;
            originalSizeDelta = Target.sizeDelta;
            capturedOriginal = true;
        }

        private void Update()
        {
            bool isPortrait = Screen.height >= Screen.width;
            if (Screen.width == lastWidth && Screen.height == lastHeight && isPortrait == lastIsPortrait) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastIsPortrait = isPortrait;
            Apply();
        }

        private void Apply()
        {
            if (Target == null || !capturedOriginal) return;
            bool isPortrait = Screen.height >= Screen.width;

            if (!isPortrait)
            {
                Target.anchorMin = originalAnchorMin;
                Target.anchorMax = originalAnchorMax;
                Target.anchoredPosition = originalAnchoredPos;
                Target.sizeDelta = originalSizeDelta;
                return;
            }

            Target.anchorMin = new Vector2(0f, originalAnchorMin.y);
            Target.anchorMax = new Vector2(1f, originalAnchorMax.y);
            Target.anchoredPosition = new Vector2(0f, originalAnchoredPos.y);
            Target.sizeDelta = new Vector2(-PortraitMargin * 2f, originalSizeDelta.y);
        }
    }
}
