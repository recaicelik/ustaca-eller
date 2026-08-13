using System.Collections.Generic;
using UnityEngine;

namespace UstacaEller.SceneRuntime
{
    /// <summary>
    /// Finds the artwork for a scene, and falls back to a flat placeholder when a
    /// sprite has not been drawn yet.
    ///
    /// The fallback is deliberate rather than defensive. A scene can be authored,
    /// validated and played before any of its art exists, so layout and mechanics stop
    /// waiting on illustration — and each finished sprite drops in with no code change.
    /// Anything still grey on screen is the to-do list.
    /// </summary>
    public sealed class SpriteLibrary
    {
        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        private static Sprite _placeholder;

        /// <summary>Sprites live at Resources/Art/&lt;sceneId&gt;/&lt;spriteName&gt;.</summary>
        public Sprite Find(string sceneId, string spriteName)
        {
            string path = $"Art/{sceneId}/{spriteName}";
            if (_cache.TryGetValue(path, out Sprite cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>(path);
            _cache[path] = sprite;
            return sprite;
        }

        public bool Has(string sceneId, string spriteName) => Find(sceneId, spriteName) != null;

        /// <summary>A one pixel white square, scaled to whatever size the object needs.</summary>
        public static Sprite Placeholder()
        {
            if (_placeholder != null) return _placeholder;

            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _placeholder = Sprite.Create(texture, new UnityEngine.Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _placeholder.hideFlags = HideFlags.HideAndDontSave;
            return _placeholder;
        }
    }
}
