using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UstacaEller.Core.Manifest;
using UstacaEller.Editor;
using UstacaEller.SceneRuntime;

namespace UstacaEller.Tests.EditMode
{
    /// <summary>
    /// Proves the content pipeline reaches Unity intact: real manifests, synced the way
    /// a build syncs them, deserialized by Newtonsoft rather than the System.Text.Json
    /// the .NET suite uses, and turned into actual GameObjects.
    ///
    /// The .NET contract tests cover the model. These cover everything after it.
    /// </summary>
    public class SceneBuilderTests
    {
        private SceneCatalog _catalog;
        private BuiltScene _built;

        [OneTimeSetUp]
        public void SyncContent()
        {
            // Runs the same sync a build runs, so a broken sync fails here rather than
            // on a device.
            ContentSync.Sync();
            _catalog = new SceneCatalog();
        }

        [SetUp]
        public void BuildKitchen()
        {
            _built = new SceneBuilder().Build(_catalog.Load("kitchen"));
        }

        [TearDown]
        public void CleanUp()
        {
            if (_built?.Root != null) Object.DestroyImmediate(_built.Root);
        }

        [Test]
        public void ContentSyncPutsScenesWhereTheRuntimeLooks()
        {
            CollectionAssert.Contains(_catalog.SceneIds().ToList(), "kitchen");
        }

        [Test]
        public void NewtonsoftReadsTheManifestTheSameWayTheDotnetSuiteDoes()
        {
            SceneManifest manifest = _catalog.Load("kitchen");

            Assert.AreEqual("kitchen", manifest.Id);
            Assert.AreEqual("scene.kitchen.title", manifest.TitleKey);
            Assert.AreEqual(1920f, manifest.Canvas.Width);
            Assert.AreEqual(0.08f, manifest.Objects.Single(o => o.Id == "dough_a").Cut.MinPieceArea, 0.0001f);
            Assert.AreEqual(150f, manifest.Objects.Single(o => o.Id == "dough_a").PlaceholderSize.Width, 0.0001f);
        }

        [Test]
        public void EveryObjectZoneAndCharacterBecomesAGameObject()
        {
            // Counts come from the manifest rather than a literal: the invariant is that
            // nothing is dropped on the way in, not that the kitchen has sixteen props.
            SceneManifest manifest = _built.Manifest;

            Assert.AreEqual(manifest.Objects.Count, _built.Objects.Count);
            Assert.AreEqual(manifest.Zones.Count, _built.Zones.Count);
            Assert.AreEqual(manifest.Characters.Count, _built.Characters.Count);
            Assert.IsTrue(_built.Objects.ContainsKey("dough_a"));
            Assert.IsTrue(_built.Zones.ContainsKey("shelf_grid"));
        }

        [Test]
        public void PlaceholderSizesReachTheRenderer()
        {
            // Without this every prop is the same square and the blockout cannot be
            // reviewed — a counter and a biscuit look identical.
            Vector3 counter = _built.Objects["counter"].transform.localScale;
            Vector3 cookie = _built.Objects["cookie_star"].transform.localScale;

            Assert.Greater(counter.x, cookie.x);
            Assert.AreNotEqual(counter.x, counter.y);
        }

        [Test]
        public void LayerOrderBecomesSortingOrder()
        {
            // The window is on the background layer, the dough on props. Getting this
            // backwards hides half the scene behind the wall.
            int window = _built.Objects["window"].GetComponent<SpriteRenderer>().sortingOrder;
            int dough = _built.Objects["dough_a"].GetComponent<SpriteRenderer>().sortingOrder;

            Assert.Less(window, dough);
        }

        [Test]
        public void CanvasCoordinatesFlipIntoWorldSpace()
        {
            // The window sits near the top of the canvas (y 220 of 1080), so in world
            // space — where y grows upward — it must be above the counter at y 760.
            float window = _built.Objects["window"].transform.localPosition.y;
            float counter = _built.Objects["counter"].transform.localPosition.y;

            Assert.Greater(window, counter);
        }

        [Test]
        public void TheCanvasCentreIsTheWorldOrigin()
        {
            var mapper = new CanvasMapper(_catalog.Load("kitchen").Canvas);

            Vector3 centre = mapper.ToWorld(960f, 540f);

            Assert.AreEqual(0f, centre.x, 0.0001f);
            Assert.AreEqual(0f, centre.y, 0.0001f);
        }

        [Test]
        public void NarrowScreensLetterboxInsteadOfCroppingTheSides()
        {
            var mapper = new CanvasMapper(_catalog.Load("kitchen").Canvas);

            // 16:9 is the design aspect, so it needs no adjustment.
            Assert.AreEqual(mapper.OrthographicSize, mapper.OrthographicSizeFor(16f / 9f), 0.0001f);

            // 4:3 is narrower: the view has to grow or the shelf runs off the right edge,
            // and a prop a child cannot reach is worse than one that looks small.
            Assert.Greater(mapper.OrthographicSizeFor(4f / 3f), mapper.OrthographicSize);

            // Wider than the design keeps the height fit; the extra width is just margin.
            Assert.AreEqual(mapper.OrthographicSize, mapper.OrthographicSizeFor(21f / 9f), 0.0001f);
        }

        [Test]
        public void TheWholeCanvasFitsOnACommonTabletAspect()
        {
            var mapper = new CanvasMapper(_catalog.Load("kitchen").Canvas);
            float aspect = 4f / 3f;

            float halfHeight = mapper.OrthographicSizeFor(aspect);
            float halfWidth = halfHeight * aspect;

            // The furthest corner of the design area must sit inside the view.
            Vector3 corner = mapper.ToWorld(1920f, 1080f);
            Assert.LessOrEqual(Mathf.Abs(corner.x), halfWidth + 0.0001f);
            Assert.LessOrEqual(Mathf.Abs(corner.y), halfHeight + 0.0001f);
        }

        [Test]
        public void MappingRoundTrips()
        {
            var mapper = new CanvasMapper(_catalog.Load("kitchen").Canvas);

            Vector2 back = mapper.ToCanvas(mapper.ToWorld(300f, 700f));

            Assert.AreEqual(300f, back.x, 0.001f);
            Assert.AreEqual(700f, back.y, 0.001f);
        }

        [Test]
        public void ZonesAreCentredOnTheirManifestRectangle()
        {
            SceneZone shelf = _catalog.Load("kitchen").Zones.Single(z => z.Id == "shelf_grid");
            var mapper = new CanvasMapper(_catalog.Load("kitchen").Canvas);

            Vector3 expected = mapper.ToWorld(
                shelf.Shape.X + shelf.Shape.Width * 0.5f,
                shelf.Shape.Y + shelf.Shape.Height * 0.5f);

            Assert.AreEqual(expected.x, _built.Zones["shelf_grid"].transform.localPosition.x, 0.001f);
            Assert.AreEqual(expected.y, _built.Zones["shelf_grid"].transform.localPosition.y, 0.001f);
        }

        [Test]
        public void PlaceholderColoursAreStablePerObject()
        {
            // Screenshots between builds are only comparable if a prop keeps its colour.
            Color first = _built.Objects["cake"].GetComponent<SpriteRenderer>().color;

            BuiltScene again = new SceneBuilder().Build(_catalog.Load("kitchen"));
            Color second = again.Objects["cake"].GetComponent<SpriteRenderer>().color;
            Object.DestroyImmediate(again.Root);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void LocalizationCatalogsSyncAndLoad()
        {
            LocaleSettings settings = _catalog.LoadLocaleSettings();
            var catalogs = _catalog.LoadLocalization(settings.Locales.Select(locale => locale.Code));

            Assert.AreEqual("tr", settings.SourceLocale);
            Assert.IsTrue(catalogs.ContainsKey("tr"));
            Assert.IsTrue(catalogs.ContainsKey("en"));
            Assert.AreEqual("Mutfak", catalogs["tr"]["scene.kitchen.title"]);
            Assert.AreEqual("Kitchen", catalogs["en"]["scene.kitchen.title"]);
        }
    }
}
