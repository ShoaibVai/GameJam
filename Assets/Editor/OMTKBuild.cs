using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OMTK.Editor
{
    public static class OMTKBuild
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string AndroidPath = "Builds/Android/OneMoreTimeKnight.apk";
        private const string LinuxPath = "Builds/Linux/OneMoreTimeKnight.x86_64";

        [MenuItem("OMTK/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Shoaib";
            PlayerSettings.productName = "One More Time, Knight";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel31;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.shoaib.onemoretimeknight");
            EditorUserBuildSettings.buildAppBundle = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OMTK/Build Android")]
        public static void BuildAndroid()
        {
            ApplyPlayerSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(AndroidPath) ?? "Builds/Android");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = AndroidPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Android build failed: " + report.summary.result);
            }

            Debug.Log("Android build written to " + AndroidPath + " (" + report.summary.totalSize + " bytes)");
        }

        [MenuItem("OMTK/Build Linux")]
        public static void BuildLinux()
        {
            ApplyPlayerSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(LinuxPath) ?? "Builds/Linux");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = LinuxPath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Linux build failed: " + report.summary.result);
            }

            Debug.Log("Linux build written to " + LinuxPath + " (" + report.summary.totalSize + " bytes)");
        }
    }
}
