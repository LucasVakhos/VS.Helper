using System;
using System.IO;
using System.IO.Compression;

namespace VS.Helper.Core;

/// <summary>
/// Lightweight ZIP facade for the Tool Window.
/// For full solution-aware archiving use <see cref="Zip.ZipBuildService"/>.
/// </summary>
public class ZipEngine
{
    /// <summary>Packages <paramref name="sourcePath"/> folder into <paramref name="outZip"/>.</summary>
    public void Build(string sourcePath, string outZip)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path is empty.", nameof(sourcePath));
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException("Source folder not found: " + sourcePath);
        if (string.IsNullOrWhiteSpace(outZip))
            throw new ArgumentException("Output ZIP path is empty.", nameof(outZip));

        string outDir = Path.GetDirectoryName(outZip);
        if (!string.IsNullOrWhiteSpace(outDir))
            Directory.CreateDirectory(outDir);

        if (File.Exists(outZip))
            File.Delete(outZip);

        ZipFile.CreateFromDirectory(sourcePath, outZip, CompressionLevel.Optimal, includeBaseDirectory: false);
    }
}
