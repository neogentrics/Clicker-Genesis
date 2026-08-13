using UnityEngine;
using UnityEngine.UI;

namespace ClickerGenesis.Core
{
    /// <summary>Player's explicit orientation choice from the Settings screen (2026-08-09, real
    /// mobile-device bug fix - see the "portrait UI badly broken on a real S25 Ultra" report).
    /// Auto is the default - the game follows the device's live orientation, same as most mobile
    /// apps; Portrait/Landscape are an explicit lock the player can opt into.</summary>
    public enum OrientationPreference { Auto, Portrait, Landscape }

    /// <summary>
    /// Replaces every screen's previously-static CanvasScaler reference resolution with a live,
    /// orientation-aware one - the real root cause behind the reported layout breakage. Two real
    /// bugs this fixes at once: (1) three screens (Settings/Clicker/BuyVerse) had their reference
    /// resolution hardcoded to a LANDSCAPE 1920x1080 pair while being rendered on portrait phones,
    /// and (2) even the one screen already using a portrait reference (MainMenu, 1080x1920) still
    /// broke on a real device because a real phone's aspect ratio (e.g. a Galaxy S25 Ultra's
    /// 1080x2340, notably taller than 1080x1920) never exactly matches whatever single reference
    /// pair was authored against - CanvasScaler's width/height blend then produces dead space or
    /// edge-clipping depending on which dimension the mismatch favors. A single static reference
    /// resolution, portrait or landscape, can never be "the fix" once the same UI has to render on
    /// an unknown range of real device aspect ratios AND support live rotation - this recomputes
    /// the reference resolution every time the live screen size changes instead.
    ///
    /// Add to the same GameObject as each screen's root Canvas/CanvasScaler. Reads
    /// GameSettings.Orientation (Auto/Portrait/Landscape, PlayerPrefs-persisted) - Auto follows
    /// Screen.width/Screen.height live (so device rotation reflows immediately); Portrait/Landscape
    /// pins Screen.orientation to that mode via Unity's own orientation lock AND picks the matching
    /// reference resolution, so a forced choice can't itself go stale against a rotated device.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvasController : MonoBehaviour
    {
        [Tooltip("Reference resolution used whenever the effective orientation is portrait (live aspect height > width, or the player forced Portrait).")]
        public Vector2 PortraitReference = new Vector2(1080, 1920);

        [Tooltip("Reference resolution used whenever the effective orientation is landscape (live aspect width > height, or the player forced Landscape).")]
        public Vector2 LandscapeReference = new Vector2(1920, 1080);

        private CanvasScaler scaler;
        private int lastWidth = -1;
        private int lastHeight = -1;
        private OrientationPreference lastPreference;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void OnEnable()
        {
            GameSettings.OnChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.OnChanged -= Apply;
        }

        /// <summary>Cheap per-frame check, not a full re-layout - only reacts when the live screen
        /// size actually changes (device rotation, or a desktop window resize), matching this
        /// project's own "no per-frame UI rebuild" discipline (bug #22 class).</summary>
        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || GameSettings.Orientation != lastPreference)
                Apply();
        }

        private void Apply()
        {
            // Landscape-only (2026-08-12, user's explicit call): Unity's CanvasScaler can't serve
            // one scene layout correctly in both orientations at once - fixing portrait invariably
            // broke landscape and vice versa, and duplicating every scene for a portrait-specific
            // layout was judged not worth it. The app is locked to landscape at the OS level
            // (Project Settings > Player > Allowed Orientations for Auto Rotation - Portrait and
            // Portrait Upside Down both unchecked), so Screen.height should never legitimately
            // exceed Screen.width; this just makes sure the reference resolution can't drift back
            // into a portrait layout even if that assumption is ever violated transiently.
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastPreference = GameSettings.Orientation;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referenceResolution = LandscapeReference;

            if (GameSettings.IsOrientationLockSupported)
            {
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
            }
        }
    }
}
