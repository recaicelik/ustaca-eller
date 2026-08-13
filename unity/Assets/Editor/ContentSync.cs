using System.IO;
using UnityEditor;
using UnityEngine;

namespace UstacaEller.Editor
{
    /// <summary>
    /// Copies content/ into StreamingAssets so the player can read it.
    ///
    /// content/ lives at the repository root rather than under Assets/ on purpose: a
    /// scene author edits JSON, runs the validator and never opens Unity. This step is
    /// the seam. It will be replaced by an Addressables build once scenes ship as
    /// downloadable groups; StreamingAssets is enough while everything fits in the
    /// binary.
    /// </summary>
    public static class ContentSync
    {
        private const string DestinationFolder = "content";

        public static string SourceRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content"));

        public static string DestinationRoot =>
            Path.Combine(Application.streamingAssetsPath, DestinationFolder);

        [MenuItem("Ustaca Eller/Sync content")]
        public static void SyncAndRefresh()
        {
            int files = Sync();
            AssetDatabase.Refresh();
            Debug.Log($"[Ustaca Eller] Synced {files} content file(s) into StreamingAssets.");
        }

        /// <summary>Mirrors scenes and localization catalogs. Returns the file count.</summary>
        public static int Sync()
        {
            if (Directory.Exists(DestinationRoot)) Directory.Delete(DestinationRoot, recursive: true);
            Directory.CreateDirectory(DestinationRoot);

            int count = 0;
            foreach (string folder in new[] { "scenes", "i18n" })
            {
                count += CopyTree(Path.Combine(SourceRoot, folder), Path.Combine(DestinationRoot, folder));
            }

            return count;
        }

        private static int CopyTree(string source, string destination)
        {
            if (!Directory.Exists(source)) return 0;

            Directory.CreateDirectory(destination);
            int count = 0;

            foreach (string file in Directory.GetFiles(source, "*.json"))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
                count++;
            }

            foreach (string directory in Directory.GetDirectories(source))
            {
                count += CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }

            return count;
        }
    }
}
