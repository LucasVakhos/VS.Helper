using System;
using System.IO;

namespace VS.Helper.Core.Zip;

internal static class ZipPathTools
{
    public static string ResolvePath(string baseDir, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Path.GetFullPath(baseDir);

        path = Environment.ExpandEnvironmentVariables(path);

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(baseDir, path));
    }

    public static string GetRelativePath(string baseDirectory, string filePath)
    {
        Uri baseUri = new Uri(ZipVariableResolver.AppendDirectorySeparatorChar(Path.GetFullPath(baseDirectory)));
        Uri fileUri = new Uri(Path.GetFullPath(filePath));
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    public static bool IsSamePath(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasWildcard(string path) => !string.IsNullOrEmpty(path) && (path.Contains("*") || path.Contains("?"));
}
