using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NativePrompt.Tests
{
    public sealed class NativePromptSampleExporterTests
    {
        private const string SourceAssetPath = "Assets/Samples/NativePrompt";
        private const string DestinationAssetPath =
            "Packages/com.ishix.nativeprompt/Samples~/NativePrompt";

        [Test]
        public void ExportedSampleExactlyMatchesAssetsSample()
        {
            string sourcePath = GetProjectFullPath(SourceAssetPath);
            string destinationPath = GetProjectFullPath(DestinationAssetPath);

            Assert.That(Directory.Exists(sourcePath), Is.True, $"Missing source: {sourcePath}");
            Assert.That(
                Directory.Exists(destinationPath),
                Is.True,
                $"Missing exported sample: {destinationPath}");

            string[] sourceFiles = GetRelativeFilePaths(sourcePath);
            string[] destinationFiles = GetRelativeFilePaths(destinationPath);
            Assert.That(
                destinationFiles,
                Is.EqualTo(sourceFiles),
                "The exported sample file list does not match Assets/Samples/NativePrompt.");

            foreach (string relativePath in sourceFiles)
            {
                byte[] sourceContents = File.ReadAllBytes(Path.Combine(sourcePath, relativePath));
                byte[] destinationContents = File.ReadAllBytes(
                    Path.Combine(destinationPath, relativePath));
                Assert.That(
                    destinationContents,
                    Is.EqualTo(sourceContents),
                    $"The exported sample differs: {relativePath}");
            }
        }

        private static string[] GetRelativeFilePaths(string rootPath)
        {
            return Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories)
                .Select(path => path.Substring(rootPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static string GetProjectFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
