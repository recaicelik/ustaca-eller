using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace UstacaEller.Editor
{
    /// <summary>
    /// Applies the project settings that compliance and build size depend on, and
    /// re-checks them on every editor load.
    ///
    /// These settings are written down in unity/README.md, but a checklist a human
    /// applies by hand is a checklist someone eventually half-applies. The CI gate in
    /// tools/check-compliance.mjs catches a violation when a pull request opens; this
    /// catches it the moment someone flips a toggle in the inspector.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        private const string CompanyName = "Ustaca Eller";
        private const string ProductName = "Ustaca Eller";
        private const string ApplicationIdentifier = "app.ustacaeller";

        /// <summary>
        /// Package name prefixes that must never appear in Packages/manifest.json.
        /// Kept in step with BANNED_PACKAGES in tools/check-compliance.mjs.
        /// </summary>
        private static readonly string[] BannedPackagePrefixes =
        {
            "com.unity.services.",
            "com.unity.purchasing",
            "com.unity.ads",
            "com.unity.analytics",
            "com.google.firebase",
            "com.google.play.",
            "com.appsflyer",
            "com.adjust",
            "io.branch",
            "com.facebook",
            "com.amplitude",
            "com.mixpanel",
        };

        static ProjectBootstrap()
        {
            EditorApplication.delayCall += AuditOnly;
        }

        [MenuItem("Ustaca Eller/Apply project settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            // Unity 6 allows turning the splash off even on a Personal licence. For a
            // premium kids brand the Unity logo on launch is the wrong first frame.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // A digital toy is held in two hands.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Build size. The reference device is an entry-level Android phone with
            // limited storage on a metered connection; the initial download is part of
            // the install funnel.
            PlayerSettings.stripEngineCode = true;

            foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
                PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetApplicationIdentifier(target, ApplicationIdentifier);
            }

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

            SetHardwareStatisticsSubmission(false);

            AssetDatabase.SaveAssets();
            Debug.Log("[Ustaca Eller] Project settings applied.");

            Audit();
        }

        [MenuItem("Ustaca Eller/Audit compliance")]
        public static void Audit()
        {
            var problems = new List<string>();

            if (PlayerSettings.SplashScreen.show) problems.Add("splash screen is on");
            if (!PlayerSettings.stripEngineCode) problems.Add("engine code stripping is off");

            foreach (string package in BannedPackages())
            {
                problems.Add($"banned package in Packages/manifest.json: {package}");
            }

            if (problems.Count == 0)
            {
                Debug.Log("[Ustaca Eller] Compliance audit clean.");
                return;
            }

            foreach (string problem in problems)
            {
                // An error, not a warning: every item here is either a documented Apple
                // Kids Category rejection cause or a decision the build depends on.
                Debug.LogError($"[Ustaca Eller] {problem}");
            }
        }

        private static void AuditOnly()
        {
            EditorApplication.delayCall -= AuditOnly;
            Audit();
        }

        private static IEnumerable<string> BannedPackages()
        {
            var found = new List<string>();
            string manifestPath = System.IO.Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (!System.IO.File.Exists(manifestPath)) return found;

            // Deliberately a substring scan rather than a JSON parse: this must keep
            // working even when the manifest is mid-edit and not valid JSON.
            string manifest = System.IO.File.ReadAllText(manifestPath);
            foreach (string prefix in BannedPackagePrefixes)
            {
                if (manifest.Contains($"\"{prefix}")) found.Add(prefix);
            }

            return found;
        }

        /// <summary>
        /// Turns off hardware statistics submission. There is no PlayerSettings property
        /// for it, so the serialised field is edited directly.
        /// </summary>
        private static void SetHardwareStatisticsSubmission(bool enabled)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Ustaca Eller] Could not open ProjectSettings.asset; set submitAnalytics to 0 by hand.");
                return;
            }

            var settings = new SerializedObject(assets[0]);
            SerializedProperty property = settings.FindProperty("submitAnalytics");
            if (property == null)
            {
                Debug.LogWarning("[Ustaca Eller] submitAnalytics field not found; verify it by hand.");
                return;
            }

            property.boolValue = enabled;
            settings.ApplyModifiedProperties();
        }
    }
}
