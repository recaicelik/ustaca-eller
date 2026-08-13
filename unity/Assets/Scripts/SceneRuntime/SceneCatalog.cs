using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UstacaEller.Core.Manifest;

namespace UstacaEller.SceneRuntime
{
    /// <summary>
    /// Finds and reads scene manifests and localization catalogs.
    ///
    /// Reads from StreamingAssets by default. The root is injectable so tests can point
    /// at content/ directly, and so the Addressables path can replace it later without
    /// touching callers.
    /// </summary>
    public sealed class SceneCatalog
    {
        private readonly string _contentRoot;

        public SceneCatalog(string contentRoot = null)
        {
            _contentRoot = contentRoot ?? Path.Combine(Application.streamingAssetsPath, "content");
        }

        public string ScenesRoot => Path.Combine(_contentRoot, "scenes");

        public string LocalizationRoot => Path.Combine(_contentRoot, "i18n");

        public IEnumerable<string> SceneIds()
        {
            if (!Directory.Exists(ScenesRoot)) yield break;

            foreach (string directory in Directory.GetDirectories(ScenesRoot))
            {
                if (File.Exists(Path.Combine(directory, "manifest.json"))) yield return Path.GetFileName(directory);
            }
        }

        public SceneManifest Load(string sceneId)
        {
            string path = Path.Combine(ScenesRoot, sceneId, "manifest.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"No manifest for scene '{sceneId}'.", path);

            SceneManifest manifest = JsonConvert.DeserializeObject<SceneManifest>(File.ReadAllText(path));
            if (manifest == null) throw new InvalidDataException($"{path} deserialized to null.");

            return manifest;
        }

        /// <summary>
        /// Reads every locale catalog. Missing files are skipped rather than thrown on:
        /// a locale mid-translation must not stop the game from starting.
        /// </summary>
        public Dictionary<string, IReadOnlyDictionary<string, string>> LoadLocalization(IEnumerable<string> localeCodes)
        {
            var catalogs = new Dictionary<string, IReadOnlyDictionary<string, string>>();

            foreach (string code in localeCodes)
            {
                string path = Path.Combine(LocalizationRoot, $"{code}.json");
                if (!File.Exists(path)) continue;

                var entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
                if (entries == null) continue;

                entries.Remove("$comment");
                catalogs[code] = entries;
            }

            return catalogs;
        }

        public LocaleSettings LoadLocaleSettings()
        {
            string path = Path.Combine(LocalizationRoot, "locales.json");
            if (!File.Exists(path)) throw new FileNotFoundException("locales.json is missing.", path);

            LocaleSettings settings = JsonConvert.DeserializeObject<LocaleSettings>(File.ReadAllText(path));
            if (settings == null) throw new InvalidDataException($"{path} deserialized to null.");

            return settings;
        }
    }

    public sealed class LocaleSettings
    {
        public string SourceLocale { get; set; }

        public List<string> FallbackChain { get; set; } = new List<string>();

        public List<LocaleEntry> Locales { get; set; } = new List<LocaleEntry>();
    }

    public sealed class LocaleEntry
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public bool VoiceOver { get; set; }

        /// <summary>A shipping locale must be fully translated; CI enforces it.</summary>
        public bool Shipping { get; set; }
    }
}
