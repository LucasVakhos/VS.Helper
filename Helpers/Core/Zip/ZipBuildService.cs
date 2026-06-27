using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildService
{
    public ZipBuildResult Build(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            throw new InvalidOperationException("Нет открытого Solution.");

        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        string configPath = Path.Combine(solutionDir, ZipBuildConfig.FileName);
        bool generated = !File.Exists(configPath);

        ZipBuildConfig config = ZipBuildConfig.LoadOrCreateDefault(solutionPath);
        if (generated)
            config.Save(solutionPath);

        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        string rootDir = ResolvePath(solutionDir, ExpandVariables(config.Root, solutionDir, solutionName));
        if (!Directory.Exists(rootDir))
            throw new InvalidOperationException("Корневая папка ZIP-конфига не найдена: " + rootDir);

        string outputDir = ResolvePath(solutionDir, ExpandVariables(config.OutputDir, solutionDir, solutionName));
        Directory.CreateDirectory(outputDir);

        string archiveName = ExpandVariables(config.ArchiveName, solutionDir, solutionName);
        if (!archiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            archiveName += ".zip";

        string zipPath = Path.Combine(outputDir, archiveName);
        string stagingRoot = Path.Combine(Path.GetTempPath(), "VS.Helper.Zip.NewEngine", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);

        try
        {
            List<string> selectedFiles = CollectFiles(solutionPath, rootDir, config)
                .Select(Path.GetFullPath)
                .Where(File.Exists)
                .Where(x => !IsSamePath(x, zipPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<string> manifestFiles = new();
            foreach (string file in selectedFiles)
            {
                string relative = ZipPath.IsInside(rootDir, file)
                    ? ZipPath.GetRelativePath(rootDir, file)
                    : Path.Combine("_External", Path.GetFileName(file));

                relative = ZipPath.NormalizeRelative(relative);
                if (string.IsNullOrWhiteSpace(relative) || ZipIgnoreMatcher.IsExcluded(relative, config.Exclude))
                    continue;

                string destination = Path.Combine(stagingRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? stagingRoot);
                File.Copy(file, destination, true);
                manifestFiles.Add(relative);
            }

            if (config.IncludeManifest)
                ZipManifestWriter.Write(stagingRoot, solutionPath, rootDir, manifestFiles);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            CreateArchiveFromManifest(stagingRoot, zipPath);

            return new ZipBuildResult
            {
                ZipPath = zipPath,
                FileCount = manifestFiles.Count,
                ConfigPath = configPath,
                UsedGeneratedConfig = generated
            };
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
        }
    }

    private static void CreateArchiveFromManifest(string stagingRoot, string zipPath)
    {
        using FileStream stream = new(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using ZipArchive archive = new(stream, ZipArchiveMode.Create);

        foreach (string file in Directory.EnumerateFiles(stagingRoot, "*.*", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            string relative = ZipPath.NormalizeRelative(ZipPath.GetRelativePath(stagingRoot, file));
            ZipArchiveEntry entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
            entry.LastWriteTime = File.GetLastWriteTime(file);

            using Stream input = File.OpenRead(file);
            using Stream output = entry.Open();
            input.CopyTo(output);
        }
    }

    private static IEnumerable<string> CollectFiles(string solutionPath, string rootDir, ZipBuildConfig config)
    {
        if (config.IncludeSolutionFiles)
            yield return solutionPath;

        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        string configPath = Path.Combine(solutionDir, ZipBuildConfig.FileName);
        if (File.Exists(configPath))
            yield return configPath;

        foreach (string include in config.Include)
            foreach (string file in ExpandInclude(rootDir, include))
                yield return file;

        if (config.IncludeProjectClosure)
        {
            ProjectClosureScanner scanner = new();
            foreach (string file in scanner.Collect(solutionPath, config, rootDir))
                yield return file;
        }
    }

    private static IEnumerable<string> ExpandInclude(string rootDir, string include)
    {
        if (string.IsNullOrWhiteSpace(include))
            yield break;

        include = include.Replace('/', Path.DirectorySeparatorChar);
        if (!include.Contains("*") && !include.Contains("?"))
        {
            string full = ResolvePath(rootDir, include);
            if (File.Exists(full))
                yield return full;
            else if (Directory.Exists(full))
            {
                foreach (string file in Directory.EnumerateFiles(full, "*.*", SearchOption.AllDirectories))
                    yield return Path.GetFullPath(file);
            }
            yield break;
        }

        string searchRoot = rootDir;
        string pattern = include;
        SearchOption option = SearchOption.TopDirectoryOnly;

        if (include.Contains("**"))
        {
            int idx = include.IndexOf("**", StringComparison.Ordinal);
            string prefix = include.Substring(0, idx).TrimEnd(Path.DirectorySeparatorChar);
            if (!string.IsNullOrWhiteSpace(prefix))
                searchRoot = Path.Combine(rootDir, prefix);
            pattern = Path.GetFileName(include);
            option = SearchOption.AllDirectories;
        }
        else
        {
            int star = include.IndexOfAny(new[] { '*', '?' });
            int slash = include.LastIndexOf(Path.DirectorySeparatorChar, Math.Max(0, star));
            if (slash >= 0)
            {
                searchRoot = Path.Combine(rootDir, include.Substring(0, slash));
                pattern = include.Substring(slash + 1);
            }
        }

        if (!Directory.Exists(searchRoot))
            yield break;

        foreach (string file in Directory.EnumerateFiles(searchRoot, pattern, option))
            yield return Path.GetFullPath(file);
    }

    private static string ExpandVariables(string value, string solutionDir, string solutionName)
    {
        string nowDate = DateTime.Now.ToString("yyyyMMdd");
        string nowTime = DateTime.Now.ToString("HHmmss");

        return (value ?? string.Empty)
            .Replace("$(SolutionDir)", solutionDir)
            .Replace("$(SolutionName)", solutionName)
            .Replace("$(Date)", nowDate)
            .Replace("$(Time)", nowTime);
    }

    private static string ResolvePath(string baseDir, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return baseDir;

        string expanded = Environment.ExpandEnvironmentVariables(path);
        return Path.GetFullPath(Path.IsPathRooted(expanded) ? expanded : Path.Combine(baseDir, expanded));
    }

    private static bool IsSamePath(string left, string right)
        => string.Equals(Path.GetFullPath(left).TrimEnd('\\', '/'), Path.GetFullPath(right).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch { }
    }
}
