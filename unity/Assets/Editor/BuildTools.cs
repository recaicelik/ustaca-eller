using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UstacaEller.Editor
{
    /// <summary>
    /// Player builds driven from the command line.
    ///
    /// The iOS simulator needs its own SDK: a default Unity iOS build targets device
    /// arm64 and will not install on a simulator at all. That switch is the only thing
    /// separating "we can look at this on a phone-shaped screen today" from "we need a
    /// provisioning profile and a device first".
    /// </summary>
    public static class BuildTools
    {
        private const string MainScene = "Assets/Scenes/Main.unity";

        public static void BuildIosSimulator()
        {
            ContentSync.Sync();

            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            // Nothing is signed for a simulator, and asking Xcode to sign would need a
            // team id this project does not have yet.
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;

            // Unity defaults simulator builds to x86_64, which produces an Xcode project
            // that cannot run on an Apple silicon Mac at all — xcodebuild reports no
            // matching destination. There is no PlayerSettings property for this, so the
            // serialised field is set directly. 0 = x86_64, 1 = arm64, 2 = universal.
            SetProjectSetting("iOSSimulatorArchitecture", SimulatorArchitectureArm64);

            Build(BuildTarget.iOS, OutputPath("ios-simulator"));
        }

        private const int SimulatorArchitectureArm64 = 1;

        private static void SetProjectSetting(string property, int value)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[Ustaca Eller] Could not open ProjectSettings.asset to set {property}.");
                return;
            }

            var settings = new SerializedObject(assets[0]);
            SerializedProperty serialized = settings.FindProperty(property);
            if (serialized == null)
            {
                Debug.LogError($"[Ustaca Eller] ProjectSettings has no field named {property}.");
                return;
            }

            serialized.intValue = value;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        public static void BuildMacStandalone()
        {
            ContentSync.Sync();
            Build(BuildTarget.StandaloneOSX, Path.Combine(OutputPath("macos"), "UstacaEller.app"));
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = outputPath,
                target = target,
                // Development keeps the profiler available, which is the whole point of
                // getting onto a real device early.
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Ustaca Eller] {target} build {summary.result} after {summary.totalTime}.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[Ustaca Eller] {target} build succeeded in {summary.totalTime}: {outputPath}");
            EditorApplication.Exit(0);
        }

        private static string OutputPath(string folder) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", folder));
    }
}
