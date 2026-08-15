#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace AIMAR.Editor
{
    public static class AIMARAndroidBuild
    {
        private const string ScenePath = "Assets/AIMAR/Scenes/Entrenamiento.unity";

        [MenuItem("AIM-AR/Build Android APK")]
        public static void BuildAndroidApk()
        {
            BuildAndroidBatch();
        }

        public static void BuildAndroidBatch()
        {
            string outputDirectory = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android"));
            string outputPath = Path.Combine(outputDirectory, "AIM-AR.apk");
            Directory.CreateDirectory(outputDirectory);

            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.Android,
                new[] { GraphicsDeviceType.OpenGLES3 });

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(
                $"AIM-AR Android build: {summary.result}. " +
                $"Output={outputPath}, Size={summary.totalSize}, Time={summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Android build failed: {summary.result} ({summary.totalErrors} errors).");
            }
        }
    }
}
#endif
