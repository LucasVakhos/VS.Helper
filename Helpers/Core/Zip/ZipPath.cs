using System;
using System.IO;

namespace VS.Helper.Core.Zip;

internal static class ZipPath
{
    public static string GetRelativePath(string basePath, string path)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            return path;

        Uri baseUri = new(AppendDirectorySeparatorChar(Path.GetFullPath(basePath)));
        Uri pathUri = new(Path.GetFullPath(path));
        string relative = Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString());
        return relative.Replace('/', Path.DirectorySeparatorChar);
    }

    public static string NormalizeRelative(string path)
        => (path ?? string.Empty).Replace('\\', '/').TrimStart('/');

    public static bool IsInside(string root, string path)
    {
        string fullRoot = AppendDirectorySeparatorChar(Path.GetFullPath(root));
        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendDirectorySeparatorChar(string path)
        => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
}
