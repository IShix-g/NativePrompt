using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NativePrompt.Editor
{
    /// <summary>
    /// Batch-mode entry points for NativePrompt release verification builds.
    /// </summary>
    public static class NativePromptBuild
    {
        private const string AndroidOutputEnvironmentVariable =
            "NATIVEPROMPT_ANDROID_BUILD_PATH";
        private const string IosOutputEnvironmentVariable =
            "NATIVEPROMPT_IOS_BUILD_PATH";
        private const string VerificationScenePath =
            "Assets/Samples/NativePrompt/NativePromptSample.unity";

        /// <summary>
        /// Builds an Android APK with API level 24 as the minimum SDK.
        /// </summary>
        public static void BuildAndroid()
        {
            if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel24)
            {
                throw new BuildFailedException(
                    $"Android minimum SDK must be API 24, but was " +
                    $"{PlayerSettings.Android.minSdkVersion}.");
            }

            BuildPlayer(
                BuildTarget.Android,
                ResolveOutputPath(
                    AndroidOutputEnvironmentVariable,
                    Path.Combine("Build", "Android", "NativePrompt.apk")));
        }

        /// <summary>
        /// Generates an iOS Xcode project with iOS 13.0 as the deployment target.
        /// </summary>
        public static void BuildIos()
        {
            if (!Version.TryParse(PlayerSettings.iOS.targetOSVersionString, out Version targetVersion) ||
                targetVersion.Major != 13 ||
                targetVersion.Minor != 0)
            {
                throw new BuildFailedException(
                    $"iOS deployment target must be 13.0, but was " +
                    $"{PlayerSettings.iOS.targetOSVersionString}.");
            }

            BuildPlayer(
                BuildTarget.iOS,
                ResolveOutputPath(
                    IosOutputEnvironmentVariable,
                    Path.Combine("Build", "iOS")));
        }

        private static void BuildPlayer(BuildTarget target, string outputPath)
        {
            string[] scenes = GetVerificationScenes();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes are configured for the player build.");
            }

            PrepareOutputDirectory(outputPath, target);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"{target} build failed with result {report.summary.result} and " +
                    $"{report.summary.totalErrors} error(s). See the Unity log for details.");
            }

            Debug.Log(
                $"NativePrompt {target} build succeeded: {outputPath} " +
                $"({report.summary.totalSize} bytes, {report.summary.totalTime}).");
        }

        private static string[] GetVerificationScenes()
        {
            var scenes = new List<string>();
            bool hasVerificationScene = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    if (scene.path == VerificationScenePath)
                    {
                        scenes.Insert(0, scene.path);
                        hasVerificationScene = true;
                    }
                    else
                    {
                        scenes.Add(scene.path);
                    }
                }
            }

            if (!hasVerificationScene)
            {
                throw new BuildFailedException(
                    $"NativePrompt verification scene is not enabled: {VerificationScenePath}");
            }

            return scenes.ToArray();
        }

        private static string ResolveOutputPath(string environmentVariable, string defaultPath)
        {
            string configuredPath = Environment.GetEnvironmentVariable(environmentVariable);
            string path = string.IsNullOrWhiteSpace(configuredPath) ? defaultPath : configuredPath;
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        private static void PrepareOutputDirectory(string outputPath, BuildTarget target)
        {
            string directory = target == BuildTarget.Android
                ? Path.GetDirectoryName(outputPath)
                : outputPath;
            if (string.IsNullOrEmpty(directory))
            {
                throw new BuildFailedException($"Invalid build output path: {outputPath}");
            }

            Directory.CreateDirectory(directory);
        }
    }
}
