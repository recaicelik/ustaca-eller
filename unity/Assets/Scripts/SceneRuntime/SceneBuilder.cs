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

        /// <summary>Objects still waiting on artwork. Empty means the scene is fully drawn.</summary>
        public List<string> UndrawnObjects { get; } = new List<string>();
    }

    /// <summary>
    /// Turns a manifest into a live scene.
    ///
    /// Every object is placed and sized from the manifest, then given its artwork if
    /// that artwork exists. Anything not yet drawn appears as a flat coloured quad, so
    /// a scene is playable and measurable before illustration finishes and each new
    /// sprite drops in without a code change.
    /// </summary>
    public sealed class SceneBuilder
    {
        /// <summary>Fallback edge length in canvas pixels when a manifest states no placeholder size.</summary>
        public const float DefaultPlaceholderSize = 120f;

        private readonly SpriteLibrary _sprites = new SpriteLibrary();

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

        private GameObject BuildObject(SceneObject sceneObject, int sortingOrder, BuiltScene built)
        {
            var go = new GameObject(sceneObject.Id);
            go.transform.SetParent(built.Root.transform, worldPositionStays: false);
            go.transform.localPosition = built.Mapper.ToWorld(sceneObject.Transform);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -sceneObject.Transform.Rotation);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;

            Sprite drawn = _sprites.Find(built.Manifest.Id, sceneObject.SpriteName);
            if (drawn == null) built.UndrawnObjects.Add(sceneObject.Id);

            renderer.sprite = drawn != null ? drawn : SpriteLibrary.Placeholder();
            renderer.color = drawn != null ? Color.white : ColourFor(sceneObject.Id);

            float width = sceneObject.PlaceholderSize?.Width ?? DefaultPlaceholderSize;
            float height = sceneObject.PlaceholderSize?.Height ?? DefaultPlaceholderSize;
            Fit(go.transform, renderer.sprite, built.Mapper, width, height, sceneObject.Transform.Scale);

            return go;
        }

        private GameObject BuildZone(SceneZone zone, BuiltScene built, bool visible)
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
            renderer.sprite = SpriteLibrary.Placeholder();
            renderer.color = ZoneColour(zone.Type);
            renderer.sortingOrder = 90;

            return go;
        }

        private GameObject BuildCharacter(SceneCharacter character, BuiltScene built)
        {
            var go = new GameObject($"character:{character.Id}");
            go.transform.SetParent(built.Root.transform, worldPositionStays: false);
            go.transform.localPosition = built.Mapper.ToWorld(character.Transform.X, character.Transform.Y);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 100;

            // Rive and Spine arrive later; this is the still pose that holds the anchor.
            Sprite drawn = _sprites.Find(built.Manifest.Id, character.Id);
            renderer.sprite = drawn != null ? drawn : SpriteLibrary.Placeholder();
            renderer.color = drawn != null ? Color.white : new Color(1f, 1f, 1f, 0.6f);

            float size = DefaultPlaceholderSize * 1.5f;
            Fit(go.transform, renderer.sprite, built.Mapper, size, size, character.Transform.Scale);

            return go;
        }

        /// <summary>
        /// Scales a transform so the sprite covers exactly the requested canvas size,
        /// whatever pixels-per-unit that sprite was imported at. Without this, swapping a
        /// placeholder for real art changes the object's size on screen.
        /// </summary>
        private static void Fit(Transform transform, Sprite sprite, CanvasMapper mapper, float width, float height, float scale)
        {
            Vector2 native = sprite.bounds.size;
            if (native.x <= 0f || native.y <= 0f) return;

            transform.localScale = new Vector3(
                mapper.ToWorldLength(width) * scale / native.x,
                mapper.ToWorldLength(height) * scale / native.y,
                1f);
        }

        /// <summary>One colour per zone kind, so a blockout screenshot reads at a glance.</summary>
        private static Color ZoneColour(string zoneType) => zoneType switch
        {
            ZoneKind.Snap => new Color(0.2f, 0.6f, 1f, 0.22f),
            ZoneKind.Grid => new Color(1f, 0.75f, 0.1f, 0.22f),
            ZoneKind.PaintArea => new Color(0.2f, 0.85f, 0.4f, 0.22f),
            _ => new Color(0.5f, 0.5f, 0.5f, 0.22f),
        };

        /// <summary>Stable per id, so an undrawn prop keeps its colour across runs.</summary>
        private static Color ColourFor(string id)
        {
            int hash = 17;
            foreach (char character in id) hash = unchecked(hash * 31 + character);

            float hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.45f, 0.85f);
        }
    }
}
