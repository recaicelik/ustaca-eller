using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UstacaEller.SceneRuntime;

namespace UstacaEller.Editor
{
    /// <summary>
    /// Creates the playable scene asset and renders greybox screenshots without opening
    /// the editor window.
    ///
    /// The screenshot matters more than it sounds. Until art exists there is nothing to
    /// look at in a build, but layout mistakes — a prop behind a wall, a zone off the
    /// canvas — are instant to spot in a picture and invisible in a passing test.
    /// </summary>
    public static class SceneTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Ustaca Eller/Create main scene")]
        public static void CreateMainScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var bootstrapObject = new GameObject("Scene Bootstrap");
            bootstrapObject.AddComponent<SceneBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log($"[Ustaca Eller] Created {ScenePath} and set it as the only build scene.");
        }

        [MenuItem("Ustaca Eller/Render screenshots")]
        public static void RenderScreenshot()
        {
            RenderScreenshot("kitchen", 1920, 1080);
        }

        /// <summary>
        /// Builds a scene from its manifest and writes a PNG. Called from the command
        /// line by tools/screenshot.mjs, so failures must be loud.
        /// </summary>
        public static void RenderScreenshot(string sceneId, int width, int height)
        {
            ContentSync.Sync();

            // Two images, because they answer different questions. Without the zone
            // overlays you can judge whether the scene looks right; with them you can
            // judge whether it plays right. One picture cannot do both — the overlays
            // tint everything underneath them.
            Capture(sceneId, width, height, showZones: false, suffix: string.Empty);
            Capture(sceneId, width, height, showZones: true, suffix: "-zones");
        }

        private static void Capture(string sceneId, int width, int height, bool showZones, string suffix)
        {
            var cameraObject = new GameObject("Screenshot Camera");
            var bootstrapObject = new GameObject("Screenshot Bootstrap");
            RenderTexture target = null;
            BuiltScene built = null;

            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                var bootstrap = bootstrapObject.AddComponent<SceneBootstrap>();

                built = new SceneBuilder { ShowZones = showZones }.Build(new SceneCatalog().Load(sceneId));
                bootstrap.FrameCamera(camera, built);

                target = new RenderTexture(width, height, 24) { antiAliasing = 2 };
                camera.targetTexture = target;
                camera.Render();

                var image = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;

                string path = OutputPath(sceneId + suffix);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);

                Debug.Log($"[Ustaca Eller] Screenshot written: {path}");
            }
            finally
            {
                if (built?.Root != null) Object.DestroyImmediate(built.Root);
                Object.DestroyImmediate(bootstrapObject);
                Object.DestroyImmediate(cameraObject);
                if (target != null) Object.DestroyImmediate(target);
            }
        }

        /// <summary>Command-line entry point. Reads -sceneId from the arguments.</summary>
        public static void RenderScreenshotFromCommandLine()
        {
            string sceneId = "kitchen";
            string[] arguments = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == "-sceneId") sceneId = arguments[i + 1];
            }

            RenderScreenshot(sceneId, 1920, 1080);
        }

        private static string OutputPath(string name) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "screenshots", $"{name}.png"));
    }
}
