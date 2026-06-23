// Helpers\VSHelperEngine.cs
// Commands\VSHelperEngine.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.Commands;

internal static class VSHelperEngine
{
    private static readonly string[] IgnoredDirectoryNames =
    {
        "bin", "obj", ".vs", ".git", "node_modules", "packages"
    };

    public static string Run(VSHelperOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SearchPath))
            options.SearchPath = options.SolutionDir;

        StringBuilder log = new StringBuilder();

        log.AppendLine("VS.Helper / VSHelper");
        log.AppendLine("Solution: " + options.SolutionPath);
        log.AppendLine("Операция: " + options.Item);
        log.AppendLine("Search: " + options.SearchPath);
        log.AppendLine("Place: " + options.PlacePath);
        log.AppendLine("Pattern: " + options.Pattern);
        log.AppendLine("Backup: " + options.UseBackup);
        log.AppendLine("DryRun: " + options.DryRun);
        log.AppendLine();

        switch (options.Item)
        {
            case VSHelperComboTodoItems.DeleteEmpty:
                ProcessTextFiles(options, log, RemoveExtraEmptyLines);
                break;

            case VSHelperComboTodoItems.DeleteRegionRows:
                ProcessTextFiles(options, log, RemoveRegionLines);
                break;

            case VSHelperComboTodoItems.FindAndReplace:
                FindAndReplace(options, log);
                break;

            case VSHelperComboTodoItems.FindValueOrClassAddScaveToProject:
                FindValueOrClassAddScaveToProject(options, log);
                break;

            case VSHelperComboTodoItems.ClearNameSpace:
                ProcessTextFiles(options, log, ClearDuplicateUsingBlocks);
                break;

            case VSHelperComboTodoItems.CollectAllNameSpaces:
                CollectRegex(options, log, @"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)", "namespaces.txt");
                break;

            case VSHelperComboTodoItems.CollectUsingPackages:
                CollectRegex(options, log, @"^\s*using\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;", "usings.txt");
                break;

            case VSHelperComboTodoItems.DeleteBakFiles:
                DeleteBakFiles(options, log);
                break;

            case VSHelperComboTodoItems.DeleteNonProjectFiles:
                DeleteNonProjectFiles(options, log);
                break;

            case VSHelperComboTodoItems.SyncProjectFileWithSample:
                SyncProjectFileWithSample(options, log);
                break;

            case VSHelperComboTodoItems.ConvertOldCsprojToSdkStyle:
                ConvertOldCsprojToSdkStyle(options, log);
                break;

            case VSHelperComboTodoItems.TranslateEnToRu:
                log.AppendLine("Перевод через AI/API в VS.Helper пока не подключён. Операция оставлена в списке как зарезервированная.");
                break;

            case VSHelperComboTodoItems.NormalizeMethodSignatures:
                ProcessTextFiles(options, log, NormalizeMethodSignatures);
                break;

            case VSHelperComboTodoItems.RestoreCSharpFilesFromBak:
                RestoreCSharpFilesFromBak(options, log);
                break;

            case VSHelperComboTodoItems.RestoreMissingUsings:
                RestoreMissingUsings(options, log);
                break;

            case VSHelperComboTodoItems.AddFilePathCommentToCsFiles:
                AddFilePathCommentToCsFiles(options, log);
                break;

            case VSHelperComboTodoItems.CreateVsHelperZipConfig:
                CreateOrUpdateVsHelperZipConfig(options, log);
                break;

            case VSHelperComboTodoItems.BuildVsHelperZip:
                log.AppendLine("Сборка ZIP уже есть отдельной командой VS.Helper: Build ZIP. В диалоге операция оставлена как напоминание.");
                break;

            case VSHelperComboTodoItems.CommitPullPushWithToken:
                log.AppendLine("Git Sync уже есть отдельной командой VS.Helper: Commit + Sync Git. В диалоге операция оставлена как напоминание.");
                break;

            default:
                log.AppendLine("Операция пока не реализована.");
                break;
        }

        return log.ToString();
    }

    private static void ProcessTextFiles(VSHelperOptions options, StringBuilder log, Func<string, string> transform)
    {
        int changed = 0;
        int total = 0;

        foreach (string file in EnumerateFiles(options.SearchPath, options.Pattern))
        {
            total++;

            try
            {
                Encoding encoding = DetectEncoding(file);
                string oldText = File.ReadAllText(file, encoding);
                string newText = transform(oldText);

                if (oldText == newText)
                {
                    log.AppendLine("[skip] " + Short(options, file));
                    continue;
                }

                changed++;

                if (!options.DryRun)
                {
                    Backup(file, options);
                    File.WriteAllText(file, newText, encoding);
                }

                log.AppendLine("[changed] " + Short(options, file));
            }
            catch (Exception ex)
            {
                log.AppendLine("[error] " + file + " - " + ex.Message);
            }
        }

        log.AppendLine();
        log.AppendLine("Файлов просмотрено: " + total);
        log.AppendLine("Изменено: " + changed);
    }

    private static void FindAndReplace(VSHelperOptions options, StringBuilder log)
    {
        if (string.IsNullOrEmpty(options.FindText))
        {
            log.AppendLine("Поле 'Найти' пустое.");
            return;
        }

        ProcessTextFiles(options, log, text => text.Replace(options.FindText, options.ReplaceText ?? string.Empty));
    }

    private static string RemoveExtraEmptyLines(string text)
    {
        string newline = GetNewLine(text);
        string[] lines = SplitLinesNoKeep(text);
        List<string> output = new List<string>();
        bool previousBlank = false;

        foreach (string line in lines)
        {
            bool blank = string.IsNullOrWhiteSpace(line) || line.Trim() == ";";

            if (blank)
            {
                if (!previousBlank)
                    output.Add(string.Empty);

                previousBlank = true;
            }
            else
            {
                output.Add(line.Trim() == ";" ? string.Empty : line);
                previousBlank = false;
            }
        }

        return string.Join(newline, output).TrimEnd() + newline;
    }

    private static string RemoveRegionLines(string text)
    {
        string newline = GetNewLine(text);
        IEnumerable<string> lines = SplitLinesNoKeep(text)
            .Where(line =>
            {
                string trim = line.TrimStart();
                return !trim.StartsWith("#region", StringComparison.OrdinalIgnoreCase) &&
                       !trim.StartsWith("#endregion", StringComparison.OrdinalIgnoreCase);
            });

        return string.Join(newline, lines) + newline;
    }

    private static string ClearDuplicateUsingBlocks(string text)
    {
        string newline = GetNewLine(text);
        string[] lines = SplitLinesNoKeep(text);
        HashSet<string> seenUsings = new HashSet<string>(StringComparer.Ordinal);
        List<string> output = new List<string>();

        foreach (string line in lines)
        {
            string trim = line.Trim();

            if (trim.StartsWith("using ", StringComparison.Ordinal) && trim.EndsWith(";", StringComparison.Ordinal))
            {
                if (!seenUsings.Add(trim))
                    continue;
            }

            output.Add(line);
        }

        return string.Join(newline, output) + newline;
    }

    private static void CollectRegex(VSHelperOptions options, StringBuilder log, string pattern, string outputFileName)
    {
        Regex regex = new Regex(pattern, RegexOptions.Multiline);
        SortedSet<string> values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in EnumerateFiles(options.SearchPath, "*.cs"))
        {
            string text = File.ReadAllText(file, DetectEncoding(file));

            foreach (Match match in regex.Matches(text))
                values.Add(match.Groups[1].Value);
        }

        string outputDir = Directory.Exists(options.PlacePath) ? options.PlacePath : options.SolutionDir;
        Directory.CreateDirectory(outputDir);

        string outputFile = Path.Combine(outputDir, outputFileName);

        if (!options.DryRun)
            File.WriteAllLines(outputFile, values, Encoding.UTF8);

        log.AppendLine("Найдено: " + values.Count);
        log.AppendLine("Файл: " + outputFile);

        foreach (string value in values.Take(200))
            log.AppendLine(value);
    }

    private static void DeleteBakFiles(VSHelperOptions options, StringBuilder log)
    {
        int count = 0;

        foreach (string file in EnumerateFiles(options.SearchPath, "*.bak"))
        {
            count++;
            log.AppendLine("[delete] " + Short(options, file));

            if (!options.DryRun)
                File.Delete(file);
        }

        log.AppendLine("Удалено .bak: " + count);
    }

    private static void RestoreCSharpFilesFromBak(VSHelperOptions options, StringBuilder log)
    {
        int count = 0;

        foreach (string bak in EnumerateFiles(options.SearchPath, "*.bak"))
        {
            string target = bak.EndsWith(".cs.bak", StringComparison.OrdinalIgnoreCase)
                ? bak.Substring(0, bak.Length - 4)
                : null;

            if (string.IsNullOrWhiteSpace(target))
                continue;

            count++;
            log.AppendLine("[restore] " + Short(options, bak) + " -> " + Short(options, target));

            if (!options.DryRun)
                File.Copy(bak, target, true);
        }

        log.AppendLine("Восстановлено: " + count);
    }

    private static void AddFilePathCommentToCsFiles(VSHelperOptions options, StringBuilder log)
    {
        int changed = 0;
        string root = options.SolutionDir;

        foreach (string file in EnumerateFiles(options.SearchPath, "*.cs"))
        {
            Encoding encoding = DetectEncoding(file);
            string text = File.ReadAllText(file, encoding);
            string relativePath = VSHelperToolsHelper.MakeRelativePath(root, file).Replace("/", "\\");
            string comment = "// " + relativePath;

            if (text.StartsWith(comment + "\r\n", StringComparison.Ordinal) ||
                text.StartsWith(comment + "\n", StringComparison.Ordinal))
            {
                log.AppendLine("[skip] " + relativePath);
                continue;
            }

            changed++;
            log.AppendLine("[comment] " + relativePath);

            if (!options.DryRun)
            {
                Backup(file, options);
                File.WriteAllText(file, comment + Environment.NewLine + text, encoding);
            }
        }

        log.AppendLine("Добавлено комментариев: " + changed);
    }

    private static string NormalizeMethodSignatures(string source)
    {
        string newline = GetNewLine(source);

        source = Regex.Replace(source, @";\s+(?=(public|private|protected|internal)\s+)", ";" + newline + "    ", RegexOptions.Multiline);
        source = Regex.Replace(source, @"\}\s+(?=(public|private|protected|internal)\s+)", "}" + newline + newline + "    ", RegexOptions.Multiline);
        source = Regex.Replace(
            source,
            @"(?<=[^\r\n])\s+(?=(public|private|protected|internal)\s+(override|virtual|static|async|sealed|partial|new|unsafe|extern|abstract)?\s*[\w<>\[\],?.]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\()",
            newline + "    ",
            RegexOptions.Multiline);

        return source;
    }

    private static void FindValueOrClassAddScaveToProject(VSHelperOptions options, StringBuilder log)
    {
        string find = options.FindText;

        if (string.IsNullOrWhiteSpace(find))
        {
            log.AppendLine("Поле 'Найти' пустое.");
            return;
        }

        string outputDir = Directory.Exists(options.PlacePath) ? options.PlacePath : Path.Combine(options.SolutionDir, "_Found");
        Directory.CreateDirectory(outputDir);

        int count = 0;

        foreach (string file in EnumerateFiles(options.SearchPath, options.Pattern))
        {
            string text = File.ReadAllText(file, DetectEncoding(file));

            if (text.IndexOf(find, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            string destination = Path.Combine(outputDir, Path.GetFileName(file));
            count++;
            log.AppendLine("[copy] " + Short(options, file) + " -> " + destination);

            if (!options.DryRun)
                File.Copy(file, destination, true);
        }

        log.AppendLine("Скопировано: " + count);
    }

    private static void DeleteNonProjectFiles(VSHelperOptions options, StringBuilder log)
    {
        string projectPath = options.SearchPath;

        if (Directory.Exists(projectPath))
            projectPath = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
        {
            log.AppendLine("Не найден .csproj. Выбери проект из открытого .slnx.");
            return;
        }

        string projectDir = Path.GetDirectoryName(projectPath);
        HashSet<string> projectFiles = ReadProjectFiles(projectPath);
        int deleted = 0;

        foreach (string file in EnumerateFiles(projectDir, "*.cs"))
        {
            string relative = VSHelperToolsHelper.MakeRelativePath(projectDir, file).Replace("/", "\\");

            if (projectFiles.Contains(relative))
                continue;

            deleted++;
            log.AppendLine("[delete non-project] " + relative);

            if (!options.DryRun)
            {
                Backup(file, options);
                File.Delete(file);
            }
        }

        log.AppendLine("Файлов вне проекта: " + deleted);
    }

    private static void SyncProjectFileWithSample(VSHelperOptions options, StringBuilder log)
    {
        if (!File.Exists(options.SearchPath) || !File.Exists(options.PlacePath))
        {
            log.AppendLine("Выбери Project и Sample project из открытого .slnx.");
            return;
        }

        XDocument target = XDocument.Load(options.SearchPath);
        XDocument sample = XDocument.Load(options.PlacePath);

        HashSet<string> sampleCompile = ReadProjectItems(sample, "Compile");
        HashSet<string> targetCompile = ReadProjectItems(target, "Compile");

        log.AppendLine("Compile в образце: " + sampleCompile.Count);
        log.AppendLine("Compile в целевом: " + targetCompile.Count);

        foreach (string missing in sampleCompile.Except(targetCompile, StringComparer.OrdinalIgnoreCase))
            log.AppendLine("[missing in target] " + missing);

        foreach (string extra in targetCompile.Except(sampleCompile, StringComparer.OrdinalIgnoreCase))
            log.AppendLine("[extra in target] " + extra);

        log.AppendLine("Безопасный режим: операция пока только сравнивает .csproj и пишет лог.");
    }

    private static void ConvertOldCsprojToSdkStyle(VSHelperOptions options, StringBuilder log)
    {
        if (!File.Exists(options.SearchPath))
        {
            log.AppendLine("Выбери старый .csproj из открытого .slnx.");
            return;
        }

        string output = File.Exists(options.PlacePath)
            ? options.PlacePath
            : (!string.IsNullOrWhiteSpace(options.PlacePath) && options.PlacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                ? options.PlacePath
                : Path.Combine(Path.GetDirectoryName(options.SearchPath), Path.GetFileNameWithoutExtension(options.SearchPath) + ".Sdk.csproj"));

        XDocument oldDoc = XDocument.Load(options.SearchPath);

        string outputType = ReadFirst(oldDoc, "OutputType");
        string rootNamespace = ReadFirst(oldDoc, "RootNamespace");
        string assemblyName = ReadFirst(oldDoc, "AssemblyName");

        XDocument sdk = new XDocument(
            new XElement("Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk.WindowsDesktop"),
                new XElement("PropertyGroup",
                    new XElement("TargetFramework", "net8.0-windows"),
                    new XElement("UseWindowsForms", "true"),
                    string.IsNullOrWhiteSpace(outputType) ? null : new XElement("OutputType", outputType),
                    string.IsNullOrWhiteSpace(rootNamespace) ? null : new XElement("RootNamespace", rootNamespace),
                    string.IsNullOrWhiteSpace(assemblyName) ? null : new XElement("AssemblyName", assemblyName)
                )
            )
        );

        log.AppendLine("[convert] " + options.SearchPath + " -> " + output);

        if (!options.DryRun)
        {
            Backup(options.SearchPath, options);
            sdk.Save(output);
        }

        log.AppendLine("Создан базовый SDK-style .csproj. Проверь PackageReference/ресурсы вручную.");
    }

    private static void RestoreMissingUsings(VSHelperOptions options, StringBuilder log)
    {
        log.AppendLine("Восстановление using требует анализа sample project и Roslyn.");
        log.AppendLine("В этой VS.Helper-версии операция зарезервирована и не меняет файлы.");
    }

    private static void CreateOrUpdateVsHelperZipConfig(VSHelperOptions options, StringBuilder log)
    {
        string root = options.SolutionDir;
        string configPath = Path.Combine(root, "VS.Helper.Zip.xml");

        XDocument doc;

        if (File.Exists(configPath))
        {
            doc = XDocument.Load(configPath);

            if (doc.Root == null)
                doc.Add(new XElement("VSHelperZip"));

            if (!string.Equals(doc.Root.Name.LocalName, "VSHelperZip", StringComparison.Ordinal))
            {
                XElement oldRoot = doc.Root;
                XElement newRoot = new XElement("VSHelperZip", oldRoot.Nodes());
                doc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);
            }
        }
        else
        {
            doc = CreateDefaultVsHelperZipConfig(options);
        }

        XElement rootElement = doc.Root;
        EnsureElement(rootElement, "Git");
        XElement git = rootElement.Element("Git");
        EnsureElement(git, "UserName").Value = string.IsNullOrWhiteSpace(EnsureElement(git, "UserName").Value) ? "YOUR_GITHUB_LOGIN" : EnsureElement(git, "UserName").Value;
        EnsureElement(git, "Token");
        EnsureElement(git, "TokenProtected");

        if (!options.DryRun)
            doc.Save(configPath);

        log.AppendLine("Config: " + configPath);
        log.AppendLine("Секция <Git> создана/проверена.");
    }

    private static XDocument CreateDefaultVsHelperZipConfig(VSHelperOptions options)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("VSHelperZip",
                new XElement("Root", "$(SolutionDir)"),
                new XElement("OutputDir", "$(SolutionDir)"),
                new XElement("ArchiveName", Path.GetFileNameWithoutExtension(options.SolutionPath) + ".zip"),
                new XElement("StartProject", ""),
                new XElement("Git",
                    new XElement("UserName", "YOUR_GITHUB_LOGIN"),
                    new XElement("Token", ""),
                    new XElement("TokenProtected", "")),
                new XElement("Include",
                    new XElement("Path", Path.GetFileName(options.SolutionPath))),
                new XElement("Exclude",
                    new XElement("Path", "**/bin/**"),
                    new XElement("Path", "**/obj/**"),
                    new XElement("Path", "**/.vs/**"),
                    new XElement("Path", "**/.git/**"),
                    new XElement("Path", "**/node_modules/**"),
                    new XElement("Path", "**/packages/**"),
                    new XElement("Path", "**/*.user"),
                    new XElement("Path", "**/*.suo"),
                    new XElement("Path", "**/*.pdb"),
                    new XElement("Path", "**/*.cache"),
                    new XElement("Path", "**/*.log"),
                    new XElement("Path", "**/appsettings.Development.json"),
                    new XElement("Path", "**/appsettings.Production.json"),
                    new XElement("Path", "**/*.db"),
                    new XElement("Path", "**/*.sqlite"))
            )
        );
    }

    private static XElement EnsureElement(XElement parent, string name)
    {
        XElement element = parent.Element(name);

        if (element == null)
        {
            element = new XElement(name, string.Empty);
            parent.Add(element);
        }

        return element;
    }

    private static HashSet<string> ReadProjectFiles(string projectPath)
    {
        XDocument document = XDocument.Load(projectPath);
        HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string itemName in new[] { "Compile", "EmbeddedResource", "Content", "None" })
        {
            foreach (string item in ReadProjectItems(document, itemName))
                files.Add(item.Replace("/", "\\"));
        }

        return files;
    }

    private static HashSet<string> ReadProjectItems(XDocument document, string itemName)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (XElement element in document.Descendants().Where(x => x.Name.LocalName == itemName))
        {
            string include = ((string)element.Attribute("Include")) ?? ((string)element.Attribute("Update"));

            if (!string.IsNullOrWhiteSpace(include))
                result.Add(include.Trim());
        }

        return result;
    }

    private static string ReadFirst(XDocument document, string name)
    {
        XElement element = document.Descendants().FirstOrDefault(x => x.Name.LocalName == name);
        return element == null ? null : element.Value.Trim();
    }

    private static IEnumerable<string> EnumerateFiles(string path, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            pattern = "*.cs";

        if (File.Exists(path))
            return new[] { path };

        if (!Directory.Exists(path))
            return Enumerable.Empty<string>();

        return Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories)
            .Where(file => !IsIgnored(file));
    }

    private static bool IsIgnored(string file)
    {
        if (VSHelperToolsHelper.IsIgnoredByPath(file))
            return true;

        string name = Path.GetFileName(file);

        return name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static void Backup(string file, VSHelperOptions options)
    {
        if (!options.UseBackup || options.DryRun)
            return;

        string bak = file + ".bak";

        if (!File.Exists(bak))
            File.Copy(file, bak, false);
    }

    private static Encoding DetectEncoding(string file)
    {
        byte[] bom = new byte[4];

        using (FileStream stream = File.OpenRead(file))
            stream.Read(bom, 0, 4);

        if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
            return Encoding.UTF7;

        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
            return Encoding.UTF8;

        if (bom[0] == 0xff && bom[1] == 0xfe)
            return Encoding.Unicode;

        if (bom[0] == 0xfe && bom[1] == 0xff)
            return Encoding.BigEndianUnicode;

        return new UTF8Encoding(false);
    }

    private static string GetNewLine(string text)
    {
        return text.Contains("\r\n") ? "\r\n" : "\n";
    }

    private static string[] SplitLinesNoKeep(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
    }

    private static string Short(VSHelperOptions options, string file)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.SolutionDir) &&
                file.StartsWith(options.SolutionDir, StringComparison.OrdinalIgnoreCase))
            {
                return VSHelperToolsHelper.MakeRelativePath(options.SolutionDir, file);
            }
        }
        catch
        {
        }

        return file;
    }
}
