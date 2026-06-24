using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VS.Helper.Core.Zip;

internal static class ZipBuildManifestWriter
{
    public const string ManifestFileName = "_VS.Helper.ZipManifest.txt";

    public static string Write(string tempRoot, string solutionPath, string rootDir, IEnumerable<string> files)
    {
        string manifestPath = Path.Combine(tempRoot, ManifestFileName);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("VS.Helper ZIP Manifest");
        sb.AppendLine("Scheme: config/project-closure, non-destructive");
        sb.AppendLine("Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Solution: " + solutionPath);
        sb.AppendLine("Root: " + rootDir);
        sb.AppendLine();
        sb.AppendLine("Files:");

        foreach (string file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine("- " + file);

        File.WriteAllText(manifestPath, sb.ToString(), new UTF8Encoding(true));
        return manifestPath;
    }
}
