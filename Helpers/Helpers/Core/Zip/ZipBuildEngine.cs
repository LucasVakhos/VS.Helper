using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildEngine
{
    private readonly ZipBuildConfigStore _configStore = new ZipBuildConfigStore();
    private readonly ZipProjectGraphReader _projectGraphReader = new ZipProjectGraphReader();

    public ZipBuildResult Build(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            throw new InvalidOperationException("Нет открытого Solution.");

        string solutionDir = Path.GetDirectoryName(solutionPath);
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        string configPath = _configStore.GetConfigPath(solutionPath);
        ZipBuildConfig config = _configStore.LoadOrCreateInMemory(solutionPath);

        string rootDir = ZipPathTools.ResolvePath(solutionDir, ZipVariableResolver.Replace(config.Root, solutionDir, solutionName));
        if (!Directory.Exists(rootDir))
            throw new InvalidOperationException("Корневая папка из конфига не найдена:\n" + rootDir);

        string outputDir = string.IsNullOrWhiteSpace(config.OutputDir)
            ? solutionDir
            : ZipPathTools.ResolvePath(solutionDir, ZipVariableResolver.Replace(config.OutputDir, solutionDir, solutionName));

        string archiveName = string.IsNullOrWhiteSpace(config.ArchiveName)
            ? solutionName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip"
            : ZipVariableResolver.Replace(config.ArchiveName, solutionDir, solutionName);

        if (!archiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            archiveName += ".zip";

        Directory.CreateDirectory(outputDir);
        string zipPath = Path.Combine(outputDir, archiveName);
        string tempRoot = Path.Combine(Path.GetTempPath(), "VS.Helper.Zip", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            IReadOnlyList<string> sourceFiles = CollectSourceFiles(solutionPath, rootDir, config, zipPath);
            List<string> archivedRelativeFiles = new List<string>();

            foreach (string file in sourceFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(file) || ZipPathTools.IsSamePath(file, zipPath))
                    continue;

                string relativePath = ZipPathTools.GetRelativePath(rootDir, file);
                if (relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || relativePath.Equals("..", StringComparison.Ordinal))
                    relativePath = Path.Combine("_External", Path.GetFileName(file));

                if (ZipPathMatcher.IsIgnoredPath(file) || ZipPathMatcher.IsExcluded(relativePath, config.Exclude))
                    continue;

                string destination = Path.Combine(tempRoot, relativePath);
                string destinationDirectory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                File.Copy(file, destination, true);
                archivedRelativeFiles.Add(relativePath.Replace('\\', '/'));
            }

            if (config.IncludeManifest)
                ZipBuildManifestWriter.Write(tempRoot, solutionPath, rootDir, archivedRelativeFiles);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempRoot, zipPath, CompressionLevel.Optimal, false);
            return new ZipBuildResult(zipPath, configPath, archivedRelativeFiles);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    public string CreateConfig(string solutionPath, bool overwrite)
    {
        return _configStore.WriteDefaultConfig(solutionPath, overwrite);
    }

    private IReadOnlyList<string> CollectSourceFiles(string solutionPath, string rootDir, ZipBuildConfig config, string zipPath)
    {
        HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string include in config.Include)
        {
            foreach (string file in ExpandConfiguredPath(rootDir, include))
                files.Add(file);
        }

        if (config.IncludeProjectClosure)
        {
            foreach (string projectPath in _projectGraphReader.GetProjectClosure(solutionPath, config.StartProject))
            {
                files.Add(projectPath);

                foreach (string file in _projectGraphReader.GetFilesFromProject(projectPath))
                    files.Add(file);
            }
        }

        files.RemoveWhere(file => ZipPathTools.IsSamePath(file, zipPath));
        return files.ToArray();
    }

    private IEnumerable<string> ExpandConfiguredPath(string rootDir, string include)
    {
        if (string.IsNullOrWhiteSpace(include))
            yield break;

        include = include.Replace('/', Path.DirectorySeparatorChar);

        if (ZipPathTools.HasWildcard(include))
        {
            foreach (string file in ExpandWildcard(rootDir, include))
                yield return file;
            yield break;
        }

        string fullPath = ZipPathTools.ResolvePath(rootDir, include);

        if (File.Exists(fullPath))
        {
            yield return fullPath;
            yield break;
        }

        if (!Directory.Exists(fullPath))
            yield break;

        foreach (string file in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories))
            yield return Path.GetFullPath(file);
    }

    private IEnumerable<string> ExpandWildcard(string rootDir, string patternPath)
    {
        string normalized = patternPath.Replace('/', Path.DirectorySeparatorChar);
        string searchRoot;
        string pattern;
        SearchOption searchOption;

        int doubleStarIndex = normalized.IndexOf("**", StringComparison.Ordinal);
        if (doubleStarIndex >= 0)
        {
            string rootPart = normalized.Substring(0, doubleStarIndex).TrimEnd(Path.DirectorySeparatorChar);
            searchRoot = string.IsNullOrWhiteSpace(rootPart) ? rootDir : Path.Combine(rootDir, rootPart);
            pattern = Path.GetFileName(normalized);
            searchOption = SearchOption.AllDirectories;
        }
        else
        {
            int firstWildcard = normalized.IndexOfAny(new[] { '*', '?' });
            int separatorBeforeWildcard = normalized.LastIndexOf(Path.DirectorySeparatorChar, Math.Max(0, firstWildcard));

            searchRoot = separatorBeforeWildcard >= 0 ? Path.Combine(rootDir, normalized.Substring(0, separatorBeforeWildcard)) : rootDir;
            pattern = separatorBeforeWildcard >= 0 ? normalized.Substring(separatorBeforeWildcard + 1) : normalized;
            searchOption = SearchOption.TopDirectoryOnly;
        }

        if (!Directory.Exists(searchRoot))
            yield break;

        foreach (string file in Directory.EnumerateFiles(searchRoot, pattern, searchOption))
            yield return Path.GetFullPath(file);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        catch
        {
            // Временная папка не должна ломать успешный ZIP.
        }
    }
}
