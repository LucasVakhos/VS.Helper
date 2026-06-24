// Helpers\VSHelperToolsHelper.cs
// Commands\VSHelperToolsHelper.cs
using System;
using EnvDTE;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VS.Helper.Commands;

internal static class VSHelperToolsHelper
{
    public static bool TryGetOpenedSolution(out VSHelperSolutionInfo info)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        info = null;

        DTE dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
        string solutionPath = dte?.Solution?.FullName;

        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return false;

        string ext = Path.GetExtension(solutionPath);

        if (!string.Equals(ext, ".sln", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase))
            return false;

        info = new VSHelperSolutionInfo
        {
            SolutionPath = solutionPath,
            SolutionDir = Path.GetDirectoryName(solutionPath),
            SolutionName = Path.GetFileName(solutionPath)
        };

        return true;
    }

    public static string[] GetProjectsFromSolution(VSHelperSolutionInfo solution)
    {
        if (solution == null || string.IsNullOrWhiteSpace(solution.SolutionDir) || !Directory.Exists(solution.SolutionDir))
            return Array.Empty<string>();

        return Directory
            .EnumerateFiles(solution.SolutionDir, "*.csproj", SearchOption.AllDirectories)
            .Where(x => !IsIgnoredByPath(x))
            .OrderBy(x => MakeRelativePath(solution.SolutionDir, x), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool IsIgnoredByPath(string file)
    {
        string[] ignored =
        {
            "bin", "obj", ".vs", ".git", "node_modules", "packages"
        };

        string[] parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return parts.Any(part => ignored.Any(x => string.Equals(x, part, StringComparison.OrdinalIgnoreCase)));
    }

    public static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        System.Windows.Forms.MessageBox.Show(
            message,
            "VS.Helper",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
    }

    public static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        System.Windows.Forms.MessageBox.Show(
            message,
            "VS.Helper / VSHelper",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }

    public static string MakeRelativePath(string baseDirectory, string filePath)
    {
        Uri baseUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(baseDirectory)));
        Uri fileUri = new Uri(Path.GetFullPath(filePath));

        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    public static string AppendDirectorySeparatorChar(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
