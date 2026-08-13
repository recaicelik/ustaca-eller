using System.Collections.Generic;

namespace UstacaEller.Core.Manifest
{
    /// <summary>
    /// The typed shape of content/scenes/&lt;id&gt;/manifest.json.
    ///
    /// Deliberately free of serializer attributes. Unity deserializes these with
    /// Newtonsoft and the test suite uses System.Text.Json; both map by property name,
    /// and keeping the types plain means neither library leaks into the core assembly.
    ///
    /// Validity is not this type's job — tools/validate-scenes.mjs is the gate, and it
    /// runs in CI before anything reaches here. These classes assume valid input.
    /// </summary>
    public sealed class SceneManifest
    {
        public int SchemaVersion { get; set; }

        public string Id { get; set; }

        public string Version { get; set; }

        /// <summary>Localization key. Never literal text — see content/i18n.</summary>
        public string TitleKey { get; set; }

        public SceneBudget Budget { get; set; }

        public SceneAssets Assets { get; set; }

        public List<SceneLayer> Layers { get; set; } = new List<SceneLayer>();

        public List<SceneObject> Objects { get; set; } = new List<SceneObject>();

        public List<SceneZone> Zones { get; set; } = new List<SceneZone>();

        public List<SceneCharacter> Characters { get; set; } = new List<SceneCharacter>();

        public SceneAudio Audio { get; set; }
    }

    public sealed class SceneBudget
    {
        public int MaxActiveSprites { get; set; }

        public int MaxSkeletons { get; set; }

        public float TargetFrameMs { get; set; } = 16.7f;
    }

    public sealed class SceneAssets
    {
        public List<AtlasAsset> Atlases { get; set; } = new List<AtlasAsset>();

        public List<SkeletonAsset> Skeletons { get; set; } = new List<SkeletonAsset>();

        public List<AudioAsset> Audio { get; set; } = new List<AudioAsset>();
    }

    public sealed class AtlasAsset
    {
        public string Id { get; set; }

        public string File { get; set; }

        public List<string> Sprites { get; set; } = new List<string>();
    }

    public sealed class SkeletonAsset
    {
        public string Id { get; set; }

        public string File { get; set; }

        /// <summary>"rive" or "spine".</summary>
        public string Type { get; set; }
    }

    public sealed class AudioAsset
    {
        public string Id { get; set; }

        public string File { get; set; }

        /// <summary>One of <see cref="AudioKind"/>.</summary>
        public string Type { get; set; }

        /// <summary>
        /// Voice is the only localized audio kind; it resolves under audio/&lt;locale&gt;/.
        /// In a product whose audience cannot read, this is what actually carries the
        /// language, so mixing it up with sfx ships the wrong language silently.
        /// </summary>
        public bool IsLocalized => Type == AudioKind.Voice;
    }

    public static class AudioKind
    {
        public const string Sfx = "sfx";
        public const string Ambience = "ambience";
        public const string Voice = "voice";
    }

    public sealed class SceneLayer
    {
        public string Id { get; set; }

        public int Order { get; set; }

        public float Parallax { get; set; }
    }

    public sealed class SceneObject
    {
        public string Id { get; set; }

        public string Layer { get; set; }

        /// <summary>"atlasId:spriteName".</summary>
        public string Sprite { get; set; }

        public ObjectTransform Transform { get; set; }

        /// <summary>Voice clip spoken when the child taps this object.</summary>
        public string LabelVoice { get; set; }

        public List<string> Mechanics { get; set; } = new List<string>();

        public GlueConfig Glue { get; set; }

        public PaintConfig Paint { get; set; }

        public CutConfig Cut { get; set; }

        public BuildConfig Build { get; set; }

        public string AtlasId => SplitSprite(0);

        public string SpriteName => SplitSprite(1);

        public bool Has(string mechanic) => Mechanics != null && Mechanics.Contains(mechanic);

        private string SplitSprite(int index)
        {
            if (string.IsNullOrEmpty(Sprite)) return null;

            string[] parts = Sprite.Split(':');
            return parts.Length > index ? parts[index] : null;
        }
    }

    public static class Mechanic
    {
        public const string Glue = "glue";
        public const string Paint = "paint";
        public const string Cut = "cut";
        public const string Build = "build";
    }

    public sealed class ObjectTransform
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Rotation { get; set; }

        public float Scale { get; set; } = 1f;
    }

    public sealed class GlueConfig
    {
        public List<string> AcceptedBy { get; set; } = new List<string>();

        public string SnapSfx { get; set; }

        /// <summary>
        /// Null means "not stated", which is not the same as false. See
        /// <see cref="ReturnsOnMiss"/> for why the default matters.
        /// </summary>
        public bool? ReturnOnMiss { get; set; }

        /// <summary>
        /// Defaults to true: an object dropped somewhere invalid and left there reads
        /// as lost, and a lost toy is where a four-year-old stops playing.
        /// </summary>
        public bool ReturnsOnMiss => ReturnOnMiss ?? true;
    }

    public sealed class PaintConfig
    {
        public string MaskFile { get; set; }

        public string BrushSfx { get; set; }

        public string FillSfx { get; set; }
    }

    public sealed class CutConfig
    {
        /// <summary>Fraction of the original area; pieces below it are never produced.</summary>
        public float MinPieceArea { get; set; }

        public int MaxPieces { get; set; }

        public string CutSfx { get; set; }
    }

    public sealed class BuildConfig
    {
        public string GridZone { get; set; }

        public string SettleSfx { get; set; }
    }

    public sealed class SceneZone
    {
        public string Id { get; set; }

        /// <summary>One of <see cref="ZoneKind"/>.</summary>
        public string Type { get; set; }

        public ZoneShape Shape { get; set; }

        public List<string> Accepts { get; set; } = new List<string>();

        public ZoneGrid Grid { get; set; }

        public float SnapRadius { get; set; }
    }

    public static class ZoneKind
    {
        public const string Snap = "snap";
        public const string Grid = "grid";
        public const string PaintArea = "paintArea";
    }

    public sealed class ZoneShape
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }
    }

    public sealed class ZoneGrid
    {
        public int Columns { get; set; }

        public int Rows { get; set; }
    }

    public sealed class SceneCharacter
    {
        public string Id { get; set; }

        public string Skeleton { get; set; }

        public string Artboard { get; set; }

        public string StateMachine { get; set; }

        public CharacterTransform Transform { get; set; }

        public List<CharacterReaction> Reactions { get; set; } = new List<CharacterReaction>();
    }

    public sealed class CharacterTransform
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Scale { get; set; } = 1f;
    }

    public sealed class CharacterReaction
    {
        /// <summary>One of <see cref="ReactionTrigger"/>.</summary>
        public string On { get; set; }

        /// <summary>Object id pattern. Null or empty means any object.</summary>
        public string Target { get; set; }

        /// <summary>Rive or Spine state machine input name.</summary>
        public string Input { get; set; }

        /// <summary>Optional localized voice clip played with the reaction.</summary>
        public string Voice { get; set; }
    }

    public static class ReactionTrigger
    {
        public const string SceneEnter = "sceneEnter";
        public const string ObjectTapped = "objectTapped";
        public const string ObjectCut = "objectCut";
        public const string ObjectPainted = "objectPainted";
        public const string ObjectSnapped = "objectSnapped";
        public const string IdleTimeout = "idleTimeout";
    }

    public sealed class SceneAudio
    {
        public string Ambience { get; set; }

        public float AmbienceVolume { get; set; } = 0.5f;
    }
}
