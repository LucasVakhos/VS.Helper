using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VS.Helper.Core.Zip;

internal static class ZipManifestWriter
{
    public const string ManifestName = "_VS.Helper.ZipManifest.txt";

    public static void Write(string stagingRoot, string solutionPath, string rootDir, IEnumerable<string> files)
    {
        StringBuilder builder = new();
        builder.AppendLine("VS.Helper ZIP Manifest");
        builder.AppendLine("Scheme: NEW ZIP ENGINE");
        builder.AppendLine("CreatedUtc: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        builder.AppendLine("Solution: " + solutionPath);
        builder.AppendLine("Root: " + rootDir);
        builder.AppendLine();
        builder.AppendLine("Files:");

        foreach (string file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            builder.AppendLine(" - " + file);

        File.WriteAllText(Path.Combine(stagingRoot, ManifestName), builder.ToString(), Encoding.UTF8);
    }
}
