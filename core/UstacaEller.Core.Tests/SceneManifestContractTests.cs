using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UstacaEller.Core.Manifest;
using Xunit;

namespace UstacaEller.Core.Tests
{
    /// <summary>
    /// The contract between content/ and the runtime.
    ///
    /// tools/validate-scenes.mjs proves a manifest is internally consistent; these
    /// tests prove the C# model can actually read it. Nothing else connects the two,
    /// so a field added to the schema and forgotten here would only surface as a
    /// silently null value at runtime — on a device, in front of a child.
    ///
    /// Deserialization uses System.Text.Json here and Newtonsoft in Unity. Both map
    /// by name and ignore unknown properties, which is why the model carries no
    /// serializer attributes.
    /// </summary>
    public class SceneManifestContractTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));

        private static IEnumerable<string> ManifestPaths() =>
            Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "content", "scenes"), "manifest.json", SearchOption.AllDirectories);

        private static SceneManifest Load(string path) =>
            JsonSerializer.Deserialize<SceneManifest>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException($"{path} deserialized to null.");

        private static SceneManifest Kitchen() =>
            Load(Path.Combine(RepositoryRoot(), "content", "scenes", "kitchen", "manifest.json"));

        public static TheoryData<string> AllManifests()
        {
            var data = new TheoryData<string>();
            foreach (string path in ManifestPaths()) data.Add(path);
            return data;
        }

        [Fact]
        public void ThereIsAtLeastOneSceneToLoad()
        {
            // Guards the theory below: an empty content folder would make it pass vacuously.
            Assert.NotEmpty(ManifestPaths());
        }

        [Theory]
        [MemberData(nameof(AllManifests))]
        public void EverySceneDeserializesWithItsEssentialsIntact(string path)
        {
            SceneManifest scene = Load(path);

            Assert.Equal(1, scene.SchemaVersion);
            Assert.False(string.IsNullOrEmpty(scene.Id));
            Assert.Equal($"scene.{scene.Id}.title", scene.TitleKey);
            Assert.NotNull(scene.Budget);
            Assert.NotNull(scene.Canvas);
            Assert.True(scene.Canvas.Width > 0 && scene.Canvas.Height > 0);
            Assert.NotEmpty(scene.Layers);
            Assert.NotEmpty(scene.Objects);
            Assert.NotEmpty(scene.Assets.Atlases);

            foreach (SceneObject sceneObject in scene.Objects)
            {
                Assert.False(string.IsNullOrEmpty(sceneObject.AtlasId));
                Assert.False(string.IsNullOrEmpty(sceneObject.SpriteName));
                Assert.NotNull(sceneObject.Transform);

                // Declaring a mechanic without its config block would leave the runtime
                // holding a null it has no sensible answer for.
                if (sceneObject.Has(Mechanic.Cut)) Assert.NotNull(sceneObject.Cut);
                if (sceneObject.Has(Mechanic.Glue)) Assert.NotNull(sceneObject.Glue);
                if (sceneObject.Has(Mechanic.Paint)) Assert.NotNull(sceneObject.Paint);
                if (sceneObject.Has(Mechanic.Build)) Assert.NotNull(sceneObject.Build);
            }
        }

        [Theory]
        [MemberData(nameof(AllManifests))]
        public void EverySceneStaysWithinItsOwnBudget(string path)
        {
            SceneManifest scene = Load(path);

            Assert.True(scene.Objects.Count <= scene.Budget.MaxActiveSprites);
            Assert.True(scene.Characters.Count <= scene.Budget.MaxSkeletons);
        }

        [Fact]
        public void KitchenLoadsWithTheShapeItWasAuthoredWith()
        {
            SceneManifest scene = Kitchen();

            Assert.Equal("kitchen", scene.Id);
            Assert.Equal("0.1.0", scene.Version);
            Assert.Single(scene.Characters);

            // Layer order decides what hides what, so the orders must be distinct —
            // equal orders leave the draw order up to the engine.
            List<int> orders = scene.Layers.Select(layer => layer.Order).ToList();
            Assert.Equal(orders.Count, orders.Distinct().Count());
            Assert.Equal(orders.OrderBy(order => order), orders);

            // Every object points at a layer that exists.
            var layerIds = scene.Layers.Select(layer => layer.Id).ToHashSet();
            Assert.All(scene.Objects, sceneObject => Assert.Contains(sceneObject.Layer, layerIds));

            // Object ids must be unique — the runtime and the save file both key on them.
            Assert.Equal(scene.Objects.Count, scene.Objects.Select(o => o.Id).Distinct().Count());

            // All four mechanics appear, so this scene keeps exercising the whole runtime.
            foreach (string mechanic in new[] { Mechanic.Cut, Mechanic.Glue, Mechanic.Paint, Mechanic.Build })
            {
                Assert.Contains(scene.Objects, o => o.Has(mechanic));
            }
        }

        [Fact]
        public void CutSettingsSurvive()
        {
            SceneObject dough = Kitchen().Objects.Single(o => o.Id == "dough_a");

            Assert.True(dough.Has(Mechanic.Cut));
            Assert.Equal(0.08f, dough.Cut.MinPieceArea);
            Assert.Equal(6, dough.Cut.MaxPieces);
            Assert.Equal("sfx_cut", dough.Cut.CutSfx);
        }

        [Fact]
        public void AnUnstatedReturnOnMissStillMeansReturn()
        {
            var glue = new GlueConfig();

            // The manifest states it explicitly today, but omitting it must never turn
            // into "leave it wherever it landed" — a lost object ends the play session.
            Assert.Null(glue.ReturnOnMiss);
            Assert.True(glue.ReturnsOnMiss);
        }

        [Fact]
        public void SpriteReferencesSplitIntoAtlasAndName()
        {
            SceneObject cake = Kitchen().Objects.Single(o => o.Id == "cake");

            Assert.Equal("atlas_kitchen", cake.AtlasId);
            Assert.Equal("cake_plain", cake.SpriteName);
        }

        [Fact]
        public void OnlyVoiceClipsAreTreatedAsLocalized()
        {
            List<AudioAsset> audio = Kitchen().Assets.Audio;

            Assert.True(audio.Single(a => a.Id == "vo_dough").IsLocalized);
            Assert.False(audio.Single(a => a.Id == "sfx_cut").IsLocalized);
            Assert.False(audio.Single(a => a.Id == "amb_kitchen").IsLocalized);
        }

        [Fact]
        public void GridZonesKeepTheirDimensions()
        {
            SceneZone shelf = Kitchen().Zones.Single(z => z.Id == "shelf_grid");

            Assert.Equal(ZoneKind.Grid, shelf.Type);
            Assert.Equal(4, shelf.Grid.Columns);
            Assert.Equal(2, shelf.Grid.Rows);
            Assert.Contains("jar_*", shelf.Accepts);
        }

        [Fact]
        public void CharacterReactionsKeepTheirTriggersAndVoices()
        {
            SceneCharacter maker = Kitchen().Characters.Single();

            Assert.Equal("characters", maker.Skeleton);
            Assert.Equal("Reactions", maker.StateMachine);
            Assert.Equal(5, maker.Reactions.Count);

            CharacterReaction greeting = maker.Reactions.Single(r => r.On == ReactionTrigger.SceneEnter);
            Assert.Equal("greet", greeting.Input);
            Assert.Equal("vo_welcome", greeting.Voice);
        }
    }
}
