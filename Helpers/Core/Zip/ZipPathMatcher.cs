using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VS.Helper.Core.Zip;

internal static class ZipPathMatcher
{
    public static bool IsExcluded(string relativePath, IEnumerable<string> excludes)
    {
        string normalized = NormalizeRelativePath(relativePath);

        foreach (string exclude in excludes ?? Enumerable.Empty<string>())
        {
            string pattern = NormalizeRelativePath(exclude);

            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            if (WildcardMatch(normalized, pattern))
                return true;

            string segmentPattern = pattern.Trim('/');

            if (!segmentPattern.Contains("*") && normalized.Split('/').Any(x => x.Equals(segmentPattern, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    public static bool IsIgnoredPath(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        string[] parts = fullPath.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        string fileName = System.IO.Path.GetFileName(fullPath);

        if (fileName.EndsWith(".user", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".suo", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".cache", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            part.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
            part.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("packages", StringComparison.OrdinalIgnoreCase));
    }

    private static bool WildcardMatch(string text, string pattern)
    {
        pattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    private static string NormalizeRelativePath(string path)
    {
        return (path ?? string.Empty)
            .Replace('\\', '/')
            .TrimStart('/')
            .TrimEnd('/');
    }
}
