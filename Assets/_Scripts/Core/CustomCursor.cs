using UnityEngine;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Applies the game's custom cursor once at startup, regardless of which scene the
    /// player lands on first - RuntimeInitializeOnLoadMethod fires before any scene's Awake,
    /// so this doesn't need to be wired into any particular screen script.
    /// </summary>
    public static class CustomCursor
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            var texture = Resources.Load<Texture2D>("Cursor/cursorBasicGold");
            if (texture == null) return;
            var hotspot = new Vector2(texture.width * 0.15f, texture.height * 0.1f);
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }
    }
}
