using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// UI built at edit time via script (Instantiate/AddComponent in an editor script, no
    /// intervening domain reload) has repeatedly turned up in a subtly stale visual state that
    /// doesn't match its own correct RectTransform/text data - a Menu button's Text component
    /// serialized as disabled, a fill Image not cropping despite a correct fillAmount, list rows
    /// with correct sizes that still don't paint. Call this to force every Graphic and TMP mesh
    /// to actually repaint, instead of trusting Unity's own dirty-flagging to catch it.
    /// </summary>
    public static class UiRefreshUtil
    {
        public static void ForceFullRefresh(Transform root)
        {
            if (root == null) return;

            Canvas.ForceUpdateCanvases();

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.SetAllDirty();

            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
                tmp.ForceMeshUpdate();

            if (root is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        /// <summary>
        /// An immediate ForceFullRefresh call during Awake() verified correct in scripted testing
        /// (which has natural delay between build and check, letting Unity's engine loop run a few
        /// frames first) but was reported still blank in real, uninterrupted Play sessions - the
        /// immediate call runs before Unity's own Canvas/CanvasScaler setup has settled on the very
        /// first frame of a fresh scene load. Run this as a coroutine (host.StartCoroutine(...)) to
        /// wait for real completed render frames before forcing the correction, which is what an
        /// immediate call cannot guarantee.
        /// </summary>
        public static IEnumerator DeferredFullRefresh(Transform root)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            ForceFullRefresh(root);
        }
    }
}
