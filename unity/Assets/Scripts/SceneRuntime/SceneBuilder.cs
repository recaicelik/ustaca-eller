using System.Collections.Generic;
using UnityEngine;
using UstacaEller.Core.Manifest;

namespace UstacaEller.SceneRuntime
{
    /// <summary>What a build produced, so callers and tests can find things by id.</summary>
    public sealed class BuiltScene
    {
        public SceneManifest Manifest { get; set; }

        public CanvasMapper Mapper { get; set; }

        public GameObject Root { get; set; }

        public Dictionary<string, GameObject> Objects { get; } = new Dictionary<string, GameObject>();

        public Dictionary<string, GameObject> Zones { get; } = new Dictionary<string, GameObject>();

        public Dictionary<string, GameObject> Characters { get; } = new Dictionary<string, GameObject>();
    }

    /// <summary>
    /// Turns a manifest into a live scene.
    ///
    /// Art does not exist yet, so every object is a flat coloured quad sized from a
    /// placeholder. That is the point of a greybox: the layout, the layer order, the
    /// drop zones and all four mechanics can be exercised and measured now, and the
    /// illustrator's work drops into the same slots later without the runtime changing.
    ///
    /// Placeholder colours are derived from the object id, so the same prop is the same
    /// colour every run and screenshots are comparable between builds.
    /// </summary>
    public sealed class SceneBuilder
    {
        /// <summary>Fallback edge length in canvas pixels when a manifest states no placeholder size.</summary>
        public const float DefaultPlaceholderSize = 120f;

        private static Sprite _placeholderSprite;

        /// <summary>
        /// Draws drop zones as translucent overlays. Off in a real build — this is for
        /// blockout screenshots, where an invisible zone is exactly the mistake you are
        /// trying to catch.
        /// </summary>
        public bool ShowZones { get; set; }

        public BuiltScene Build(SceneManifest manifest, Transform parent = null)
        {
            // A manifest that parsed but came back empty means managed stripping removed
            // the property setters Newtonsoft needs. Saying so beats a NullReferenceException
            // three frames deeper, because the cause is nowhere near the symptom.
            if (manifest?.Canvas == null)
            {
                throw new System.InvalidOperationException(
                    "Scene manifest has no canvas. If this is a player build, check that Assets/link.xml " +
                    "still preserves UstacaEller.Core.Manifest — managed stripping removes the property " +
                    "setters Newtonsoft populates by reflection.");
            }

            var built = new BuiltScene
            {
                Manifest = manifest,
                Mapper = new CanvasMapper(manifest.Canvas),
                Root = new GameObject($"Scene:{manifest.Id}"),
            };

            if (parent != null) built.Root.transform.SetParent(parent, worldPositionStays: false);

            var layerOrder = new Dictionary<string, int>();
            foreach (SceneLayer layer in manifest.Layers) layerOrder[layer.Id] = layer.Order;

            foreach (SceneZone zone in manifest.Zones)
            {
                built.Zones[zone.Id] = BuildZone(zone, built, ShowZones);
            }

            foreach (SceneObject sceneObject in manifest.Objects)
            {
                layerOrder.TryGetValue(sceneObject.Layer, out int order);
                built.Objects[sceneObject.Id] = BuildObject(sceneObject, order, built);
            }

            foreach (SceneCharacter character in manifest.Characters)
            {
                built.Characters[character.Id] = BuildCharacter(character, built);
            }

            return built;
        }

        private static GameObject BuildObject(SceneObject sceneObject, int sortingOrder, BuiltScene built)
        {
            var go = new GameObject(sceneObject.Id);
            go.transform.SetParent(built.Root.transform, worldPositionStays: false);
            go.transform.localPosition = built.Mapper.ToWorld(sceneObject.Transform);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -sceneObject.Transform.Rotation);

            float width = sceneObject.PlaceholderSize?.Width ?? DefaultPlaceholderSize;
            float height = sceneObject.PlaceholderSize?.Height ?? DefaultPlaceholderSize;
            go.transform.localScale = new Vector3(
                built.Mapper.ToWorldLength(width) * sceneObject.Transform.Scale,
                built.Mapper.ToWorldLength(height) * sceneObject.Transform.Scale,
                1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprite();
            renderer.color = ColourFor(sceneObject.Id);
            renderer.sortingOrder = sortingOrder;

            return go;
        }

        private static GameObject BuildZone(SceneZone zone, BuiltScene built, bool visible)
        {
            var go = new GameObject($"zone:{zone.Id}");
            go.transform.SetParent(built.Root.transform, worldPositionStays: false);

            // Manifest zones are top-left anchored; a Unity transform is centred.
            ZoneShape shape = zone.Shape;
            go.transform.localPosition = built.Mapper.ToWorld(
                shape.X + shape.Width * 0.5f,
                shape.Y + shape.Height * 0.5f);

            go.transform.localScale = new Vector3(
                built.Mapper.ToWorldLength(shape.Width),
                built.Mapper.ToWorldLength(shape.Height),
                1f);

            if (!visible) return go;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprite();
            renderer.color = ZoneColour(zone.Type);
            renderer.sortingOrder = 90;

            return go;
        }

        /// <summary>One colour per zone kind, so a blockout screenshot reads at a glance.</summary>
        private static Color ZoneColour(string zoneType) => zoneType switch
        {
            ZoneKind.Snap => new Color(0.2f, 0.6f, 1f, 0.22f),
            ZoneKind.Grid => new Color(1f, 0.75f, 0.1f, 0.22f),
            ZoneKind.PaintArea => new Color(0.2f, 0.85f, 0.4f, 0.22f),
            _ => new Color(0.5f, 0.5f, 0.5f, 0.22f),
        };

        private static GameObject BuildCharacter(SceneCharacter character, BuiltScene built)
        {
            var go = new GameObject($"character:{character.Id}");
            go.transform.SetParent(built.Root.transform, worldPositionStays: false);
            go.transform.localPosition = built.Mapper.ToWorld(character.Transform.X, character.Transform.Y);

            // Rive and Spine both arrive later; the placeholder marks the anchor so the
            // scene reads correctly in the meantime.
            float size = built.Mapper.ToWorldLength(DefaultPlaceholderSize * 1.5f) * character.Transform.Scale;
            go.transform.localScale = new Vector3(size, size, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprite();
            renderer.color = new Color(1f, 1f, 1f, 0.6f);
            renderer.sortingOrder = 100;

            return go;
        }

        private static Sprite PlaceholderSprite()
        {
            if (_placeholderSprite != null) return _placeholderSprite;

            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _placeholderSprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _placeholderSprite.hideFlags = HideFlags.HideAndDontSave;
            return _placeholderSprite;
        }

        /// <summary>Stable per id, so a prop keeps its colour across runs and screenshots.</summary>
        private static Color ColourFor(string id)
        {
            int hash = 17;
            foreach (char character in id) hash = unchecked(hash * 31 + character);

            float hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.45f, 0.85f);
        }
    }
}
