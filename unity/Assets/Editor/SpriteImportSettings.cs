using UnityEditor;

namespace UstacaEller.Editor
{
    /// <summary>
    /// Imports everything under Resources/Art as a sprite.
    ///
    /// This project was assembled by hand rather than from the 2D template, so Unity's
    /// default texture type is not Sprite. Without this, Resources.Load&lt;Sprite&gt;
    /// returns null for perfectly good artwork and the scene silently falls back to
    /// grey boxes — a failure that looks like missing files rather than wrong settings.
    ///
    /// Doing it here rather than by hand also means regenerated art keeps its settings:
    /// build-sprites.mjs deletes and rewrites the folder on every run.
    /// </summary>
    public sealed class SpriteImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            // Sprites are authored at 2x their manifest size, so 200 units per unit of
            // manifest space keeps 1 manifest pixel equal to 1 world unit hundredth.
            importer.spritePixelsPerUnit = 200f;
            importer.mipmapEnabled = false;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
        }
    }
}
