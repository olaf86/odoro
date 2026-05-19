#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Odoro.Editor
{
    public static class CiBuild
    {
        private const string DefaultIosOutputPath = "Builds/iOS";

        [MenuItem("Odoro/CI/Build iOS Xcode Project")]
        public static void BuildIosXcodeProject()
        {
            string outputPath = GetCommandLineValue("-ciOutputPath")
                ?? Environment.GetEnvironmentVariable("CI_OUTPUT_PATH")
                ?? DefaultIosOutputPath;

            string buildNumber = GetCommandLineValue("-ciBuildNumber")
                ?? Environment.GetEnvironmentVariable("BUILD_NUMBER")
                ?? Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");

            if (!string.IsNullOrWhiteSpace(buildNumber))
            {
                PlayerSettings.iOS.buildNumber = buildNumber;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes are configured in EditorBuildSettings.");
            }

            string fullOutputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(fullOutputPath);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullOutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"iOS Xcode project build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings).");
            }

            Debug.Log($"iOS Xcode project written to {fullOutputPath}.");
        }

        private static string GetCommandLineValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif
