using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace VS.Helper.Core.OS;

public sealed class ProjectGenomeAnalyzer
{
    private static readonly Regex NamespaceRegex = new(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ClassRegex = new(@"\b(class|record|struct)\s+[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);
    private static readonly Regex InterfaceRegex = new(@"\binterface\s+[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);
    private static readonly Regex MethodLikeRegex = new(@"\b(public|private|protected|internal)\s+(static\s+)?[A-Za-z0-9_<>,.?\[\]]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\(", RegexOptions.Compiled);

    public ProjectGenome Analyze(string solutionPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            throw new FileNotFoundException("Solution file not found.", solutionPath);

        string root = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var genome = new ProjectGenome { SolutionPath = solutionPath };

        string[] projectFiles = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !IsIgnored(p))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        genome.ProjectCount = projectFiles.Length;
        genome.Projects = projectFiles.Select(p => GetRelativePathCompat(root, p)).ToList();

        var namespaceHits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var hotFiles = new List<(string File, int Score)>();

        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Where(p => !IsIgnored(p)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            int loc = text.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line));
            int classes = ClassRegex.Matches(text).Count;
            int interfaces = InterfaceRegex.Matches(text).Count;
            int methods = MethodLikeRegex.Matches(text).Count;
            int todos = CountToken(text, "TODO") + CountToken(text, "FIXME") + CountToken(text, "HACK");

            genome.SourceFileCount++;
            genome.ApproxLinesOfCode += loc;
            genome.ClassCount += classes;
            genome.InterfaceCount += interfaces;
            genome.MethodLikeCount += methods;
            genome.TodoCount += todos;

            foreach (Match match in NamespaceRegex.Matches(text))
            {
                string ns = match.Groups[1].Value.Trim();
                namespaceHits[ns] = namespaceHits.TryGetValue(ns, out int count) ? count + 1 : 1;
            }

            int score = loc + classes * 20 + methods * 3 + todos * 15;
            hotFiles.Add((GetRelativePathCompat(root, file), score));
        }

        genome.HotFiles = hotFiles.OrderByDescending(x => x.Score).ThenBy(x => x.File, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(x => x.File)
            .ToList();

        genome.TopNamespaces = namespaceHits.OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(x => x.Key + " (" + x.Value + ")")
            .ToList();

        return genome;
    }

    private static bool IsIgnored(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/.vs/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountToken(string text, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }

    private static string GetRelativePathCompat(string root, string fullPath)
    {
        try
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string normalizedFullPath = Path.GetFullPath(fullPath);

            Uri rootUri = new(normalizedRoot, UriKind.Absolute);
            Uri fileUri = new(normalizedFullPath, UriKind.Absolute);
            string relative = Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString());
            return relative.Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            return fullPath;
        }
    }
}
