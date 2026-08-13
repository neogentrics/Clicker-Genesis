using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// MainMenu's Play/Settings/Quit column and the About/description panel are individual
    /// center-anchored elements (not a panel-pair like ClickerScreen/BuyVerseScreen), each with a
    /// fixed absolute X offset from screen center (e.g. Play at x=-420, DescriptionPanel at
    /// x=+420) tuned for a wide desktop window - on a ~1080-wide portrait canvas those offsets run
    /// the elements off both edges of the screen (a 560-wide button centered at x=-420 has its
    /// left edge at x=-700, already past the -540 left edge of a 1080-wide canvas). Since these
    /// elements don't share a common parent frame the way LeftPanel/RightPanel do, PortraitPanelStack's
    /// "restack the shared frame" approach doesn't apply - this instead recenters each entry's X
    /// to 0 and target list is small enough that this level of the actual bring the entries into
    /// a real position for portrait; entries not in Entries when isPortrait keep their original spot,
    /// see the plain per-entry list below.
    /// </summary>
    public class PortraitElementReflow : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public RectTransform Target;
            [Tooltip("anchoredPosition used once stacked in portrait.")]
            public Vector2 PortraitPosition;
        }

        public Entry[] Entries;

        private Vector2[] originalPositions;
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
            if (capturedOriginal || Entries == null) return;
            originalPositions = new Vector2[Entries.Length];
            for (int i = 0; i < Entries.Length; i++)
                if (Entries[i]?.Target != null) originalPositions[i] = Entries[i].Target.anchoredPosition;
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
            if (Entries == null || !capturedOriginal) return;
            bool isPortrait = Screen.height >= Screen.width;

            for (int i = 0; i < Entries.Length; i++)
            {
                var entry = Entries[i];
                if (entry?.Target == null) continue;
                entry.Target.anchoredPosition = isPortrait ? entry.PortraitPosition : originalPositions[i];
            }
        }
    }
}
