using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Real fix for the 2026-08-12 Android alignment pass. ClickerScreen/BuyVerseScreen split the
    /// canvas into a LeftPanel/RightPanel side-by-side pair sized for a wide desktop window (roughly
    /// 42%/58% of Canvas width) - every child inside each panel (scribe rows, the Ink/Grace/Clicking
    /// Power header bar, etc.) already positions itself proportionally relative to ITS OWN panel, not
    /// the Canvas directly. On a real portrait phone that 42%/58% split leaves each panel only ~300
    /// units wide, which is narrower than the fixed pixel margins those children were built against
    /// (e.g. a scribe row's Name/Desc text box computes its width as "row width minus a ~390-unit
    /// margin for the icon and Buy button" - fine at a ~700-unit desktop row width, but NEGATIVE at a
    /// ~300-unit portrait row width, which is exactly why the Buy button visually swallowed the row's
    /// name text). Restacking the two panels vertically instead of side-by-side gives each one close
    /// to the FULL screen width again in portrait, which fixes that math without touching any of the
    /// individual row/header layouts - same "the panel IS the reference frame" property MainMenu's
    /// per-element absolute offsets don't have, which is why this component only applies to
    /// panel-pair screens, not MainMenu (fixed separately, per-element).
    /// </summary>
    public class PortraitPanelStack : MonoBehaviour
    {
        public RectTransform TopInPortrait;
        public RectTransform BottomInPortrait;

        [Tooltip("Fraction of height given to TopInPortrait once stacked (0-1). BottomInPortrait gets the remainder.")]
        [Range(0.1f, 0.9f)] public float TopHeightFraction = 0.42f;

        [Tooltip("Small gap between the two stacked panels, in canvas units.")]
        public float Gap = 12f;

        [Tooltip("Elements sized for the tall desktop panel (e.g. the big Tap button) that need to shrink once the panel is squeezed into its portrait-stacked height - restored to their original scale in landscape.")]
        public RectTransform[] ShrinkInPortrait;

        [Range(0.2f, 1f)] public float ShrinkFactor = 0.65f;

        private Vector2 topAnchorMin, topAnchorMax, topOffsetMin, topOffsetMax;
        private Vector2 bottomAnchorMin, bottomAnchorMax, bottomOffsetMin, bottomOffsetMax;
        private Vector3[] originalScales;
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
            if (capturedOriginal || TopInPortrait == null || BottomInPortrait == null) return;
            topAnchorMin = TopInPortrait.anchorMin;
            topAnchorMax = TopInPortrait.anchorMax;
            topOffsetMin = TopInPortrait.offsetMin;
            topOffsetMax = TopInPortrait.offsetMax;

            bottomAnchorMin = BottomInPortrait.anchorMin;
            bottomAnchorMax = BottomInPortrait.anchorMax;
            bottomOffsetMin = BottomInPortrait.offsetMin;
            bottomOffsetMax = BottomInPortrait.offsetMax;

            if (ShrinkInPortrait != null && ShrinkInPortrait.Length > 0)
            {
                originalScales = new Vector3[ShrinkInPortrait.Length];
                for (int i = 0; i < ShrinkInPortrait.Length; i++)
                    if (ShrinkInPortrait[i] != null) originalScales[i] = ShrinkInPortrait[i].localScale;
            }
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
            if (TopInPortrait == null || BottomInPortrait == null) return;
            bool isPortrait = Screen.height >= Screen.width;

            if (!isPortrait)
            {
                if (!capturedOriginal) return;
                TopInPortrait.anchorMin = topAnchorMin;
                TopInPortrait.anchorMax = topAnchorMax;
                TopInPortrait.offsetMin = topOffsetMin;
                TopInPortrait.offsetMax = topOffsetMax;

                BottomInPortrait.anchorMin = bottomAnchorMin;
                BottomInPortrait.anchorMax = bottomAnchorMax;
                BottomInPortrait.offsetMin = bottomOffsetMin;
                BottomInPortrait.offsetMax = bottomOffsetMax;

                if (ShrinkInPortrait != null && originalScales != null)
                    for (int i = 0; i < ShrinkInPortrait.Length; i++)
                        if (ShrinkInPortrait[i] != null) ShrinkInPortrait[i].localScale = originalScales[i];
                return;
            }

            float split = Mathf.Clamp01(TopHeightFraction);
            float halfGap = Gap * 0.5f;

            // Stretched anchors (anchorMin != anchorMax) are sized via offsetMin/offsetMax, not
            // anchoredPosition+sizeDelta - using offsets directly avoids the ambiguity of Unity's
            // anchoredPosition/sizeDelta translation for a fully stretched rect.
            TopInPortrait.anchorMin = new Vector2(0f, 1f - split);
            TopInPortrait.anchorMax = new Vector2(1f, 1f);
            TopInPortrait.offsetMin = new Vector2(0f, halfGap);
            TopInPortrait.offsetMax = new Vector2(0f, 0f);

            BottomInPortrait.anchorMin = new Vector2(0f, 0f);
            BottomInPortrait.anchorMax = new Vector2(1f, 1f - split);
            BottomInPortrait.offsetMin = new Vector2(0f, 0f);
            BottomInPortrait.offsetMax = new Vector2(0f, -halfGap);

            if (ShrinkInPortrait != null && originalScales != null)
                for (int i = 0; i < ShrinkInPortrait.Length; i++)
                    if (ShrinkInPortrait[i] != null) ShrinkInPortrait[i].localScale = originalScales[i] * ShrinkFactor;
        }
    }
}
