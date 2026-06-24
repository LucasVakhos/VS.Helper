using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VS.Helper.Core.Zip;

internal static class ZipIgnoreMatcher
{
    public static bool IsExcluded(string relativePath, IEnumerable<string> patterns)
    {
        string normalized = ZipPath.NormalizeRelative(relativePath);

        foreach (string pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            string normalizedPattern = ZipPath.NormalizeRelative(pattern.Trim());
            if (IsMatch(normalized, normalizedPattern))
                return true;
        }

        return false;
    }

    private static bool IsMatch(string value, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        if (pattern.StartsWith("**/", StringComparison.Ordinal))
        {
            string withoutPrefix = pattern.Substring(3);
            if (IsMatch(value, withoutPrefix))
                return true;
        }

        if (pattern.EndsWith("/**", StringComparison.Ordinal))
        {
            string prefix = pattern.Substring(0, pattern.Length - 3).TrimEnd('/');
            if (value.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
                value.IndexOf("/" + prefix + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        string regex = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]") + "$";

        if (Regex.IsMatch(value, regex, RegexOptions.IgnoreCase))
            return true;

        string fileName = Path.GetFileName(value);
        return Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase);
    }
}
