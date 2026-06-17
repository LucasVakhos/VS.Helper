// Commands\AppCleanerToolsCommand.cs
using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace VS.Helper.Commands;

[Command(PackageIds.AppCleanerToolsCommand)]
internal sealed class AppCleanerToolsCommand : BaseCommand<AppCleanerToolsCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Command.Enabled = HasOpenSolution();
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        DTE dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
        string solutionPath = dte?.Solution?.FullName;

        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
        {
            ShowInfo("Нет открытого Solution.");
            return;
        }

        string solutionDir = Path.GetDirectoryName(solutionPath);

        using (AppCleanerToolsDialog dialog = new AppCleanerToolsDialog(solutionDir))
        {
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                AppCleanerOptions options = dialog.GetOptions();

                if (options.Item == ComboTodoItems.DeleteNonProjectFiles)
                {
                    System.Windows.Forms.DialogResult confirm = System.Windows.Forms.MessageBox.Show(
                        "Операция может удалить файлы, которые не входят в .csproj.\n\n" +
                        "Перед удалением будут созданы .bak-копии, если включён Backup.\n\n" +
                        "Продолжить?",
                        "VS.Helper / AppCleaner",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Warning);

                    if (confirm != System.Windows.Forms.DialogResult.Yes)
                        return;
                }

                string log = AppCleanerEngine.Run(options);
                AppCleanerLogDialog.ShowLog(log);
            }
            catch (Exception ex)
            {
                ShowError(ex.ToString());
            }
        }
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        DTE dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
        string solutionPath = dte?.Solution?.FullName;

        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        System.Windows.Forms.MessageBox.Show(
            message,
            "VS.Helper",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        System.Windows.Forms.MessageBox.Show(
            message,
            "VS.Helper / AppCleaner",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }
}

#nullable disable
internal enum ComboTodoItems
{
    [ComboTodo(Name = "Удалить пустые строки...", UseBakup = true)]
    DeleteEmpty,

    [ComboTodo(Name = "Удалить строки #region #endregion...", UseBakup = true)]
    DeleteRegionRows,

    [ComboTodo(Name = "Найти и заменить...", UseBakup = true)]
    FindAndReplace,

    [ComboTodo(Name = "Найти Class или значение в Class и добавить в папку проекта...", OperationTypes = OperationTypes.ProcessFiles)]
    FindValueOrClassAddScaveToProject,

    [ComboTodo(Name = "Удалить лишние ссылки на namespace...", UseBakup = true)]
    ClearNameSpace,

    [ComboTodo(Name = "Собрать все namespace проекта...")]
    CollectAllNameSpaces,

    [ComboTodo(Name = "Собрать нужные using Packages проекта...")]
    CollectUsingPackages,

    [ComboTodo(Name = "Удалить *.bak-файлы...", OperationTypes = OperationTypes.ProcessFiles)]
    DeleteBakFiles,

    [ComboTodo(Name = "Удалить файлы не входящие в проект...", OperationTypes = OperationTypes.ProcessFiles, UseBakup = true, SearchLabel = "Cканировать Project:")]
    DeleteNonProjectFiles,

    [ComboTodo(Name = "Синхронизировать файл проекта с образцом файла проекта ...", UseBakup = true, SearchLabel = "Cканировать Project:", PlaceLabel = "Образец Project:")]
    SyncProjectFileWithSample,

    [ComboTodo(Name = "Конвертировать старый .csproj в SDK-style...", SearchLabel = "Старый Project:", PlaceLabel = "Новый Project:", UseBakup = true)]
    ConvertOldCsprojToSdkStyle,

    [ComboTodo(Name = "Перевести английский текст на русский в файлах проекта (включая комментарии)...", Pattern = PatternType.CS, UseBakup = true)]
    TranslateEnToRu,

    [ComboTodo(Name = "Нормализовать сигнатуры методов...", OperationTypes = OperationTypes.ProcessFiles, UseBakup = true)]
    NormalizeMethodSignatures,

    [ComboTodo(Name = "Восстановление файлов CSharp из Bak...")]
    RestoreCSharpFilesFromBak,

    [ComboTodo(Name = "Восстановление using в указанном проекте...", UseBakup = true, SearchLabel = "Recovery project:", PlaceLabel = "Sample project:")]
    RestoreMissingUsings,

    [ComboTodo(Name = "Добавить комментарий /*Путь к файлу*/ к файлам .сs в папке...", OperationTypes = OperationTypes.ProcessFiles)]
    AddFilePathCommentToCsFiles,

    [ComboTodo(Name = "Создать VS.Helper.Zip.xml / обновить секцию Git...")]
    CreateVsHelperZipConfig,

    [ComboTodo(Name = "Собрать ZIP по VS.Helper.Zip.xml...")]
    BuildVsHelperZip,

    [ComboTodo(Name = "Commit + Pull(Rebase) + Push через TokenProtected...")]
    CommitPullPushWithToken
}

internal enum OperationTypes
{
    ProcessContent,
    ProcessFiles,
    ProcessOther
}

internal enum PatternType
{
    [Description("*.cs")]
    CS,

    [Description("*.txt")]
    TXT,

    [Description("*.razor")]
    RAZOR,

    [Description("*.bak")]
    BAK,

    [Description("*.*")]
    ALL
}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class ComboTodoAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public PatternType Pattern { get; set; } = PatternType.CS;
    public string SearchLabel { get; set; } = "Cканировать папку:";
    public string PlaceLabel { get; set; } = "Папка для найденного:";
    public OperationTypes OperationTypes { get; set; } = OperationTypes.ProcessFiles;
    public bool UseBakup { get; set; } = false;
}

internal sealed class AppCleanerOptions
{
    public ComboTodoItems Item { get; set; }
    public string SearchPath { get; set; }
    public string PlacePath { get; set; }
    public string FindText { get; set; }
    public string ReplaceText { get; set; }
    public string Pattern { get; set; }
    public bool UseBackup { get; set; }
    public bool DryRun { get; set; }
}

internal sealed class AppCleanerToolsDialog : Form
{
    private readonly ComboBox _combo = new ComboBox();
    private readonly TextBox _searchPath = new TextBox();
    private readonly TextBox _placePath = new TextBox();
    private readonly TextBox _findText = new TextBox();
    private readonly TextBox _replaceText = new TextBox();
    private readonly ComboBox _pattern = new ComboBox();
    private readonly CheckBox _backup = new CheckBox();
    private readonly CheckBox _dryRun = new CheckBox();
    private readonly Label _searchLabel = new Label();
    private readonly Label _placeLabel = new Label();

    public AppCleanerToolsDialog(string solutionDir)
    {
        Text = "VS.Helper / AppCleaner Tools";
        Width = 760;
        Height = 390;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _combo.DropDownStyle = ComboBoxStyle.DropDownList;
        _combo.Left = 16;
        _combo.Top = 16;
        _combo.Width = 700;

        foreach (ComboTodoItems item in Enum.GetValues(typeof(ComboTodoItems)))
            _combo.Items.Add(new ComboItem(item));

        _combo.SelectedIndex = 0;
        _combo.SelectedIndexChanged += delegate { ApplySelectedMetadata(); };

        _searchLabel.Left = 16;
        _searchLabel.Top = 58;
        _searchLabel.Width = 180;

        _searchPath.Left = 200;
        _searchPath.Top = 55;
        _searchPath.Width = 430;
        _searchPath.Text = solutionDir;

        Button searchBrowse = new Button();
        searchBrowse.Left = 640;
        searchBrowse.Top = 53;
        searchBrowse.Width = 76;
        searchBrowse.Text = "...";
        searchBrowse.Click += delegate { BrowsePath(_searchPath); };

        _placeLabel.Left = 16;
        _placeLabel.Top = 92;
        _placeLabel.Width = 180;

        _placePath.Left = 200;
        _placePath.Top = 89;
        _placePath.Width = 430;
        _placePath.Text = solutionDir;

        Button placeBrowse = new Button();
        placeBrowse.Left = 640;
        placeBrowse.Top = 87;
        placeBrowse.Width = 76;
        placeBrowse.Text = "...";
        placeBrowse.Click += delegate { BrowsePath(_placePath); };

        Label findLabel = new Label();
        findLabel.Left = 16;
        findLabel.Top = 128;
        findLabel.Width = 180;
        findLabel.Text = "Найти:";

        _findText.Left = 200;
        _findText.Top = 125;
        _findText.Width = 516;

        Label replaceLabel = new Label();
        replaceLabel.Left = 16;
        replaceLabel.Top = 162;
        replaceLabel.Width = 180;
        replaceLabel.Text = "Заменить на:";

        _replaceText.Left = 200;
        _replaceText.Top = 159;
        _replaceText.Width = 516;

        Label patternLabel = new Label();
        patternLabel.Left = 16;
        patternLabel.Top = 197;
        patternLabel.Width = 180;
        patternLabel.Text = "Маска:";

        _pattern.Left = 200;
        _pattern.Top = 194;
        _pattern.Width = 120;
        _pattern.DropDownStyle = ComboBoxStyle.DropDownList;
        _pattern.Items.AddRange(new object[] { "*.cs", "*.txt", "*.razor", "*.bak", "*.*" });
        _pattern.SelectedIndex = 0;

        _backup.Left = 350;
        _backup.Top = 196;
        _backup.Width = 170;
        _backup.Text = "Создавать .bak";

        _dryRun.Left = 530;
        _dryRun.Top = 196;
        _dryRun.Width = 170;
        _dryRun.Text = "Dry Run";

        TextBox hint = new TextBox();
        hint.Left = 16;
        hint.Top = 230;
        hint.Width = 700;
        hint.Height = 70;
        hint.Multiline = true;
        hint.ReadOnly = true;
        hint.ScrollBars = ScrollBars.Vertical;
        hint.Text =
            "Поддержаны основные операции AppCleaner: строки, region, find/replace, namespace/using отчёты, .bak, комментарии пути, нормализация, базовая синхронизация csproj.\r\n" +
            "Для опасных операций сначала включи Dry Run и посмотри лог.";

        Button ok = new Button();
        ok.Left = 560;
        ok.Top = 315;
        ok.Width = 75;
        ok.Text = "Старт";
        ok.DialogResult = System.Windows.Forms.DialogResult.OK;

        Button cancel = new Button();
        cancel.Left = 642;
        cancel.Top = 315;
        cancel.Width = 75;
        cancel.Text = "Отмена";
        cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            _combo,
            _searchLabel, _searchPath, searchBrowse,
            _placeLabel, _placePath, placeBrowse,
            findLabel, _findText,
            replaceLabel, _replaceText,
            patternLabel, _pattern,
            _backup, _dryRun,
            hint,
            ok, cancel
        });

        AcceptButton = ok;
        CancelButton = cancel;

        ApplySelectedMetadata();
    }

    public AppCleanerOptions GetOptions()
    {
        ComboItem selected = (ComboItem)_combo.SelectedItem;

        return new AppCleanerOptions
        {
            Item = selected.Value,
            SearchPath = _searchPath.Text.Trim(),
            PlacePath = _placePath.Text.Trim(),
            FindText = _findText.Text,
            ReplaceText = _replaceText.Text,
            Pattern = Convert.ToString(_pattern.SelectedItem),
            UseBackup = _backup.Checked,
            DryRun = _dryRun.Checked
        };
    }

    private void ApplySelectedMetadata()
    {
        ComboItem selected = _combo.SelectedItem as ComboItem;
        if (selected == null)
            return;

        ComboTodoAttribute attr = selected.Value.GetAttribute<ComboTodoAttribute>();

        _searchLabel.Text = attr.SearchLabel;
        _placeLabel.Text = attr.PlaceLabel;
        _backup.Checked = attr.UseBakup;

        string pattern = attr.Pattern.GetDescription();
        int index = _pattern.Items.IndexOf(pattern);
        if (index >= 0)
            _pattern.SelectedIndex = index;
    }

    private static void BrowsePath(TextBox textBox)
    {
        string current = textBox.Text;

        if (!string.IsNullOrWhiteSpace(current) && File.Exists(current))
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.FileName = current;
            dialog.Filter = "All files|*.*";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox.Text = dialog.FileName;

            return;
        }

        FolderBrowserDialog folder = new FolderBrowserDialog();
        if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
            folder.SelectedPath = current;

        if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            textBox.Text = folder.SelectedPath;
    }

    private sealed class ComboItem
    {
        public ComboItem(ComboTodoItems value)
        {
            Value = value;
        }

        public ComboTodoItems Value { get; }

        public override string ToString()
        {
            ComboTodoAttribute attr = Value.GetAttribute<ComboTodoAttribute>();
            return attr == null ? Value.ToString() : attr.Name;
        }
    }
}

internal static class AppCleanerEngine
{
    private static readonly string[] IgnoredDirectoryNames =
    {
        "bin", "obj", ".vs", ".git", "node_modules", "packages"
    };

    public static string Run(AppCleanerOptions options)
    {
        StringBuilder log = new StringBuilder();

        log.AppendLine("VS.Helper / AppCleaner");
        log.AppendLine("Операция: " + options.Item);
        log.AppendLine("Search: " + options.SearchPath);
        log.AppendLine("Place: " + options.PlacePath);
        log.AppendLine("Pattern: " + options.Pattern);
        log.AppendLine("Backup: " + options.UseBackup);
        log.AppendLine("DryRun: " + options.DryRun);
        log.AppendLine();

        switch (options.Item)
        {
            case ComboTodoItems.DeleteEmpty:
                ProcessTextFiles(options, log, RemoveExtraEmptyLines);
                break;

            case ComboTodoItems.DeleteRegionRows:
                ProcessTextFiles(options, log, RemoveRegionLines);
                break;

            case ComboTodoItems.FindAndReplace:
                FindAndReplace(options, log);
                break;

            case ComboTodoItems.FindValueOrClassAddScaveToProject:
                FindValueOrClassAddScaveToProject(options, log);
                break;

            case ComboTodoItems.ClearNameSpace:
                ProcessTextFiles(options, log, ClearDuplicateUsingBlocks);
                break;

            case ComboTodoItems.CollectAllNameSpaces:
                CollectRegex(options, log, @"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)", "namespaces.txt");
                break;

            case ComboTodoItems.CollectUsingPackages:
                CollectRegex(options, log, @"^\s*using\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;", "usings.txt");
                break;

            case ComboTodoItems.DeleteBakFiles:
                DeleteBakFiles(options, log);
                break;

            case ComboTodoItems.DeleteNonProjectFiles:
                DeleteNonProjectFiles(options, log);
                break;

            case ComboTodoItems.SyncProjectFileWithSample:
                SyncProjectFileWithSample(options, log);
                break;

            case ComboTodoItems.ConvertOldCsprojToSdkStyle:
                ConvertOldCsprojToSdkStyle(options, log);
                break;

            case ComboTodoItems.TranslateEnToRu:
                log.AppendLine("Перевод через AI/API в VS.Helper пока не подключён. Операция оставлена в списке как зарезервированная.");
                break;

            case ComboTodoItems.NormalizeMethodSignatures:
                ProcessTextFiles(options, log, NormalizeMethodSignatures);
                break;

            case ComboTodoItems.RestoreCSharpFilesFromBak:
                RestoreCSharpFilesFromBak(options, log);
                break;

            case ComboTodoItems.RestoreMissingUsings:
                RestoreMissingUsings(options, log);
                break;

            case ComboTodoItems.AddFilePathCommentToCsFiles:
                AddFilePathCommentToCsFiles(options, log);
                break;

            case ComboTodoItems.CreateVsHelperZipConfig:
                CreateOrUpdateVsHelperZipConfig(options, log);
                break;

            case ComboTodoItems.BuildVsHelperZip:
                log.AppendLine("Сборка ZIP уже есть отдельной командой VS.Helper: Build ZIP. В диалоге операция оставлена как напоминание.");
                break;

            case ComboTodoItems.CommitPullPushWithToken:
                log.AppendLine("Git Sync уже есть отдельной командой VS.Helper: Commit + Sync Git. В диалоге операция оставлена как напоминание.");
                break;

            default:
                log.AppendLine("Операция пока не реализована.");
                break;
        }

        return log.ToString();
    }

    private static void ProcessTextFiles(AppCleanerOptions options, StringBuilder log, Func<string, string> transform)
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
                    log.AppendLine("[skip] " + file);
                    continue;
                }

                changed++;

                if (!options.DryRun)
                {
                    Backup(file, options);
                    File.WriteAllText(file, newText, encoding);
                }

                log.AppendLine("[changed] " + file);
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

    private static void FindAndReplace(AppCleanerOptions options, StringBuilder log)
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

    private static void CollectRegex(AppCleanerOptions options, StringBuilder log, string pattern, string outputFileName)
    {
        Regex regex = new Regex(pattern, RegexOptions.Multiline);
        SortedSet<string> values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in EnumerateFiles(options.SearchPath, "*.cs"))
        {
            string text = File.ReadAllText(file, DetectEncoding(file));

            foreach (Match match in regex.Matches(text))
                values.Add(match.Groups[1].Value);
        }

        string outputDir = Directory.Exists(options.PlacePath) ? options.PlacePath : options.SearchPath;
        Directory.CreateDirectory(outputDir);

        string outputFile = Path.Combine(outputDir, outputFileName);

        if (!options.DryRun)
            File.WriteAllLines(outputFile, values, Encoding.UTF8);

        log.AppendLine("Найдено: " + values.Count);
        log.AppendLine("Файл: " + outputFile);

        foreach (string value in values.Take(200))
            log.AppendLine(value);
    }

    private static void DeleteBakFiles(AppCleanerOptions options, StringBuilder log)
    {
        int count = 0;

        foreach (string file in EnumerateFiles(options.SearchPath, "*.bak"))
        {
            count++;
            log.AppendLine("[delete] " + file);

            if (!options.DryRun)
                File.Delete(file);
        }

        log.AppendLine("Удалено .bak: " + count);
    }

    private static void RestoreCSharpFilesFromBak(AppCleanerOptions options, StringBuilder log)
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
            log.AppendLine("[restore] " + bak + " -> " + target);

            if (!options.DryRun)
                File.Copy(bak, target, true);
        }

        log.AppendLine("Восстановлено: " + count);
    }

    private static void AddFilePathCommentToCsFiles(AppCleanerOptions options, StringBuilder log)
    {
        int changed = 0;
        string root = GetDirectoryRoot(options.SearchPath);

        foreach (string file in EnumerateFiles(options.SearchPath, "*.cs"))
        {
            Encoding encoding = DetectEncoding(file);
            string text = File.ReadAllText(file, encoding);
            string relativePath = MakeRelativePath(root, file).Replace("/", "\\");
            string comment = "//" + relativePath;

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

        source = Regex.Replace(
            source,
            @";\s+(?=(public|private|protected|internal)\s+)",
            ";" + newline + "    ",
            RegexOptions.Multiline);

        source = Regex.Replace(
            source,
            @"\}\s+(?=(public|private|protected|internal)\s+)",
            "}" + newline + newline + "    ",
            RegexOptions.Multiline);

        source = Regex.Replace(
            source,
            @"(?<=[^\r\n])\s+(?=(public|private|protected|internal)\s+(override|virtual|static|async|sealed|partial|new|unsafe|extern|abstract)?\s*[\w<>\[\],?.]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\()",
            newline + "    ",
            RegexOptions.Multiline);

        return source;
    }

    private static void FindValueOrClassAddScaveToProject(AppCleanerOptions options, StringBuilder log)
    {
        string find = options.FindText;

        if (string.IsNullOrWhiteSpace(find))
        {
            log.AppendLine("Поле 'Найти' пустое.");
            return;
        }

        string outputDir = Directory.Exists(options.PlacePath) ? options.PlacePath : Path.Combine(options.SearchPath, "_Found");
        Directory.CreateDirectory(outputDir);

        int count = 0;

        foreach (string file in EnumerateFiles(options.SearchPath, options.Pattern))
        {
            string text = File.ReadAllText(file, DetectEncoding(file));

            if (text.IndexOf(find, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            string destination = Path.Combine(outputDir, Path.GetFileName(file));
            count++;
            log.AppendLine("[copy] " + file + " -> " + destination);

            if (!options.DryRun)
                File.Copy(file, destination, true);
        }

        log.AppendLine("Скопировано: " + count);
    }

    private static void DeleteNonProjectFiles(AppCleanerOptions options, StringBuilder log)
    {
        string projectPath = options.SearchPath;

        if (Directory.Exists(projectPath))
            projectPath = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
        {
            log.AppendLine("Не найден .csproj. Укажи файл проекта или папку проекта.");
            return;
        }

        string projectDir = Path.GetDirectoryName(projectPath);
        HashSet<string> projectFiles = ReadProjectFiles(projectPath);
        int deleted = 0;

        foreach (string file in EnumerateFiles(projectDir, "*.cs"))
        {
            string relative = MakeRelativePath(projectDir, file).Replace("/", "\\");

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

    private static void SyncProjectFileWithSample(AppCleanerOptions options, StringBuilder log)
    {
        if (!File.Exists(options.SearchPath) || !File.Exists(options.PlacePath))
        {
            log.AppendLine("Укажи SearchPath = целевой .csproj, PlacePath = образец .csproj.");
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

    private static void ConvertOldCsprojToSdkStyle(AppCleanerOptions options, StringBuilder log)
    {
        if (!File.Exists(options.SearchPath))
        {
            log.AppendLine("Укажи SearchPath = старый .csproj.");
            return;
        }

        string output = File.Exists(options.PlacePath)
            ? options.PlacePath
            : (!string.IsNullOrWhiteSpace(options.PlacePath) && options.PlacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                ? options.PlacePath
                : Path.Combine(Path.GetDirectoryName(options.SearchPath), Path.GetFileNameWithoutExtension(options.SearchPath) + ".Sdk.csproj"));

        XDocument oldDoc = XDocument.Load(options.SearchPath);
        XNamespace ns = oldDoc.Root.GetDefaultNamespace();

        string outputType = ReadFirst(oldDoc, ns, "OutputType");
        string rootNamespace = ReadFirst(oldDoc, ns, "RootNamespace");
        string assemblyName = ReadFirst(oldDoc, ns, "AssemblyName");

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

    private static void RestoreMissingUsings(AppCleanerOptions options, StringBuilder log)
    {
        log.AppendLine("Восстановление using требует анализа sample project и Roslyn.");
        log.AppendLine("В этой VS.Helper-версии операция зарезервирована и не меняет файлы.");
    }

    private static void CreateOrUpdateVsHelperZipConfig(AppCleanerOptions options, StringBuilder log)
    {
        string root = GetDirectoryRoot(options.SearchPath);
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
            doc = CreateDefaultVsHelperZipConfig();
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

    private static XDocument CreateDefaultVsHelperZipConfig()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("VSHelperZip",
                new XElement("Root", "$(SolutionDir)"),
                new XElement("OutputDir", "$(SolutionDir)"),
                new XElement("ArchiveName", "$(SolutionName).zip"),
                new XElement("StartProject", "RhymeContest.Blazor.Server\\RhymeContest.Blazor.Server.csproj"),
                new XElement("Git",
                    new XElement("UserName", "YOUR_GITHUB_LOGIN"),
                    new XElement("Token", ""),
                    new XElement("TokenProtected", "")),
                new XElement("Include",
                    new XElement("Path", "RhymeContest.sln"),
                    new XElement("Path", "RhymeContest.Blazor.Server"),
                    new XElement("Path", "RhymeContest.Module"),
                    new XElement("Path", "RhymeContest.Module.Blazor"),
                    new XElement("Path", "Directory.Build.props"),
                    new XElement("Path", "Directory.Packages.props"),
                    new XElement("Path", "NuGet.config"),
                    new XElement("Path", "README.md")),
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

    private static string ReadFirst(XDocument document, XNamespace ns, string name)
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
        string[] parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (string part in parts)
        {
            if (IgnoredDirectoryNames.Any(x => string.Equals(x, part, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        string name = Path.GetFileName(file);

        return name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static void Backup(string file, AppCleanerOptions options)
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
        {
            stream.Read(bom, 0, 4);
        }

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

    private static string GetDirectoryRoot(string path)
    {
        if (Directory.Exists(path))
            return path;

        if (File.Exists(path))
            return Path.GetDirectoryName(path);

        return Environment.CurrentDirectory;
    }

    private static string MakeRelativePath(string baseDirectory, string filePath)
    {
        Uri baseUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(baseDirectory)));
        Uri fileUri = new Uri(Path.GetFullPath(filePath));

        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}

internal sealed class AppCleanerLogDialog : Form
{
    private AppCleanerLogDialog(string log)
    {
        Text = "VS.Helper / AppCleaner Log";
        Width = 980;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        TextBox textBox = new TextBox();
        textBox.Multiline = true;
        textBox.ReadOnly = true;
        textBox.ScrollBars = ScrollBars.Both;
        textBox.WordWrap = false;
        textBox.Dock = DockStyle.Fill;
        textBox.Font = new System.Drawing.Font("Consolas", 10);
        textBox.Text = log;

        Controls.Add(textBox);
    }

    public static void ShowLog(string log)
    {
        using (AppCleanerLogDialog dialog = new AppCleanerLogDialog(log))
            dialog.ShowDialog();
    }
}

internal static class EnumExtensions
{
    public static TAttribute GetAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
    {
        FieldInfo field = value.GetType().GetField(value.ToString());

        if (field == null)
            return null;

        return field.GetCustomAttributes(typeof(TAttribute), false)
            .Cast<TAttribute>()
            .FirstOrDefault();
    }

    public static string GetDescription(this Enum value)
    {
        DescriptionAttribute attr = value.GetAttribute<DescriptionAttribute>();
        return attr == null ? value.ToString() : attr.Description;
    }
}
