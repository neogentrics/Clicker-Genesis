using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Progression.SkillTreeV2
{
    /// <summary>
    /// A single connector line between two nodes, drawn as a thin rotated UI Image RectTransform -
    /// NOT a 3D LineRenderer, which renders through Canvas sorting incorrectly and doesn't respect
    /// RectMask2D clipping. The technique: pivot the Image at its own left edge, set sizeDelta.x to
    /// the distance between the two points, anchor its position at the start point, and rotate it
    /// (Z-axis) to face the end point. Standard, well-known uGUI line trick - no custom mesh
    /// generation needed.
    ///
    /// Re-tinted per state (owned/frontier/silhouette) rather than recreated - SkillTreeUIManager
    /// keeps a live reference and calls SetState() on Refresh instead of destroying/rebuilding
    /// lines every time a node's rank changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class UIConnectorLine : MonoBehaviour
    {
        public enum LineState { Owned, Frontier, Silhouette }

        /// <summary>The prerequisite node this edge originates from, and the node it feeds into -
        /// set once at creation, read back by SkillTreeUIManager.RefreshAll to re-classify the
        /// edge's state every time ranks change.</summary>
        public SkillNodeData From;
        public SkillNodeData To;

        private RectTransform rect;
        private Image image;

        private static readonly Color OwnedColor = new Color(0.95f, 0.81f, 0.42f, 0.85f);
        private static readonly Color FrontierColor = new Color(0.95f, 0.81f, 0.42f, 0.45f);
        private static readonly Color SilhouetteColor = new Color(0.27f, 0.29f, 0.36f, 0.3f);

        /// <summary>dashSprite is optional - pass a tiled dash-pattern sprite for the cross-branch
        /// "bridge" requirement style; leave null for an ordinary solid connector (the common
        /// case). Either way the line never blocks raycasts, so it can never eat a click meant for
        /// a node sitting behind or near it.</summary>
        public static UIConnectorLine Create(RectTransform parent, Sprite dashSprite, Vector2 from, Vector2 to, float thickness)
        {
            var go = new GameObject("ConnectorLine", typeof(RectTransform), typeof(Image), typeof(UIConnectorLine));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            var line = go.GetComponent<UIConnectorLine>();
            line.rect = rt;
            line.image = go.GetComponent<Image>();
            line.image.raycastTarget = false;
            if (dashSprite != null)
            {
                line.image.sprite = dashSprite;
                line.image.type = Image.Type.Tiled;
            }

            line.Reposition(from, to, thickness);
            line.SetState(LineState.Silhouette);
            return line;
        }

        public void Reposition(Vector2 from, Vector2 to, float thickness)
        {
            Vector2 dir = to - from;
            float length = Mathf.Max(dir.magnitude, 0.01f);

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = from;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetState(LineState state)
        {
            image.color = state switch
            {
                LineState.Owned => OwnedColor,
                LineState.Frontier => FrontierColor,
                _ => SilhouetteColor,
            };
        }
    }
}
