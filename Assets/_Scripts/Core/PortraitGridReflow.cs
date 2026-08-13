using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// AchievementScreen's card grid uses a GridLayoutGroup with FixedColumnCount=3 and a fixed
    /// 520-wide cell (3*520 + spacing = 1600 units minimum) - fine inside a wide desktop window,
    /// but on a ~1048-wide portrait viewport the middle column renders centered while the left and
    /// right columns spill off both edges, since GridLayoutGroup never shrinks cell size to fit its
    /// container. Drops to a single full-width column in portrait; restores the original column
    /// count/cell size in landscape.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class PortraitGridReflow : MonoBehaviour
    {
        public int PortraitColumnCount = 1;
        public float PortraitCellWidth = 1000f;
        public float PortraitCellHeight = 175f;

        private GridLayoutGroup grid;
        private int originalConstraintCount;
        private Vector2 originalCellSize;
        private bool capturedOriginal;

        private int lastWidth = -1;
        private int lastHeight = -1;
        private bool lastIsPortrait;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            CaptureOriginal();
            Apply();
        }

        private void CaptureOriginal()
        {
            if (capturedOriginal || grid == null) return;
            originalConstraintCount = grid.constraintCount;
            originalCellSize = grid.cellSize;
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
            if (grid == null || !capturedOriginal) return;
            bool isPortrait = Screen.height >= Screen.width;

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = isPortrait ? PortraitColumnCount : originalConstraintCount;
            grid.cellSize = isPortrait ? new Vector2(PortraitCellWidth, PortraitCellHeight) : originalCellSize;
        }
    }
}
