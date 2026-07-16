using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NativePrompt.Editor
{
    internal static class NativePromptSampleExporter
    {
        private const string SourceAssetPath = "Assets/Samples/NativePrompt";
        private const string DestinationAssetPath =
            "Packages/com.ishix.nativeprompt/Samples~/NativePrompt";

        private static readonly string[] AllowedAssetExtensions =
        {
            ".cs",
            ".asmdef",
            ".unity",
            ".uxml",
            ".uss",
            ".tss",
            ".asset",
            ".prefab",
            ".md"
        };

        private static readonly string[] ForbiddenFileNames =
        {
            ".env"
        };

        private static readonly string[] ForbiddenAssetExtensions =
        {
            ".key",
            ".keystore",
            ".mobileprovision",
            ".p12",
            ".pem"
        };

        [MenuItem("Window/Native Prompt/Export Sample")]
        public static void Export()
        {
            string sourceFullPath = GetProjectFullPath(SourceAssetPath);
            if (!Directory.Exists(sourceFullPath))
            {
                Debug.LogError($"[Native Prompt] Sample source does not exist: {SourceAssetPath}");
                return;
            }

            List<ExportFile> files = CollectExportFiles(sourceFullPath).ToList();
            if (files.Count == 0)
            {
                Debug.LogWarning($"[Native Prompt] Sample source has no exportable files: {SourceAssetPath}");
                return;
            }

            ReplaceDestination(files);
            AssetDatabase.Refresh();
            Debug.Log($"[Native Prompt] Sample exported to {DestinationAssetPath}");
        }

        private static IEnumerable<ExportFile> CollectExportFiles(string sourceFullPath)
        {
            foreach (string sourceFilePath in Directory.GetFiles(sourceFullPath, "*", SearchOption.AllDirectories))
            {
                string sourceAssetPath = ToAssetPath(sourceFilePath);
                string relativePath = NormalizeAssetPath(
                    sourceAssetPath.Substring(SourceAssetPath.Length).TrimStart('/'));
                if (!ShouldIncludeFile(relativePath))
                {
                    continue;
                }

                yield return new ExportFile(
                    sourceFilePath,
                    $"{DestinationAssetPath}/{relativePath}");
            }
        }

        private static bool ShouldIncludeFile(string relativePath)
        {
            string assetFileName = GetAssetFileName(relativePath);
            if (ForbiddenFileNames.Any(name =>
                    string.Equals(assetFileName, name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            string assetExtension = Path.GetExtension(assetFileName);
            if (ForbiddenAssetExtensions.Any(extension =>
                    string.Equals(assetExtension, extension, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return AllowedAssetExtensions.Any(allowed =>
                string.Equals(assetExtension, allowed, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetAssetFileName(string relativePath)
        {
            string fileName = Path.GetFileName(relativePath);
            if (fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                string assetPath = relativePath.Substring(0, relativePath.Length - ".meta".Length);
                return Path.GetFileName(assetPath);
            }

            return fileName;
        }

        private static void ReplaceDestination(IEnumerable<ExportFile> files)
        {
            string destinationFullPath = GetProjectFullPath(DestinationAssetPath);
            if (Directory.Exists(destinationFullPath))
            {
                Directory.Delete(destinationFullPath, true);
            }

            foreach (ExportFile file in files)
            {
                string destinationFilePath = GetProjectFullPath(file.DestinationAssetPath);
                string destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(file.SourceFullPath, destinationFilePath);
            }
        }

        private static string GetProjectFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static string ToAssetPath(string fullPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string relativePath = fullPath.Substring(projectRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return NormalizeAssetPath(relativePath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private readonly struct ExportFile
        {
            public readonly string SourceFullPath;
            public readonly string DestinationAssetPath;

            public ExportFile(string sourceFullPath, string destinationAssetPath)
            {
                SourceFullPath = sourceFullPath;
                DestinationAssetPath = destinationAssetPath;
            }
        }
    }
}
