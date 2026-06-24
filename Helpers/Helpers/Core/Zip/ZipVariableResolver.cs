using System;
using System.IO;

namespace VS.Helper.Core.Zip;

internal static class ZipVariableResolver
{
    public static string Replace(string value, string solutionDir, string solutionName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        DateTime now = DateTime.Now;

        return Environment.ExpandEnvironmentVariables(value)
            .Replace("$(SolutionDir)", AppendDirectorySeparatorChar(solutionDir))
            .Replace("$(SolutionName)", solutionName)
            .Replace("$(Date)", now.ToString("yyyyMMdd"))
            .Replace("$(Time)", now.ToString("HHmmss"));
    }

    public static string AppendDirectorySeparatorChar(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
