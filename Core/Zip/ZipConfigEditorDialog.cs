using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VS.Helper.Core.Zip;

internal sealed class ZipConfigEditorDialog : Form
{
    private readonly string _solutionPath;
    private readonly string _solutionDir;
    private readonly ZipBuildConfig _config;
    private readonly TextBox _txtRoot = new();
    private readonly TextBox _txtOutputDir = new();
    private readonly TextBox _txtArchiveName = new();
    private readonly TextBox _txtStartProject = new();
    private readonly CheckBox _chkProjectClosure = new();
    private readonly CheckBox _chkSolutionFiles = new();
    private readonly CheckBox _chkManifest = new();
    private readonly TreeView _tree = new();
    private readonly ListBox _excludeList = new();

    public ZipConfigEditorDialog(string solutionPath, ZipBuildConfig config)
    {
        _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
        _solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        _config = config ?? throw new ArgumentNullException(nameof(config));

        Text = "VS.Helper / ZIP config editor";
        Width = 980;
        Height = 720;
        MinimumSize = new Size(860, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = SystemFonts.MessageBoxFont;

        BuildUi();
        LoadConfigToUi();
        LoadTree();
    }

    private void BuildUi()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        GroupBox common = new() { Text = "Основные параметры", Dock = DockStyle.Top, AutoSize = true };
        TableLayoutPanel commonGrid = new() { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(8) };
        commonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        commonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        commonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        common.Controls.Add(commonGrid);

        AddLabel(commonGrid, "Root:", 0, 0); AddText(commonGrid, _txtRoot, 1, 0);
        AddLabel(commonGrid, "OutputDir:", 2, 0); AddText(commonGrid, _txtOutputDir, 3, 0);
        AddLabel(commonGrid, "ArchiveName:", 0, 1); AddText(commonGrid, _txtArchiveName, 1, 1);
        AddLabel(commonGrid, "StartProject:", 2, 1); AddText(commonGrid, _txtStartProject, 3, 1);

        _chkProjectClosure.Text = "Автоматически включать проекты решения и ProjectReference";
        _chkProjectClosure.AutoSize = true;
        commonGrid.Controls.Add(_chkProjectClosure, 0, 2);
        commonGrid.SetColumnSpan(_chkProjectClosure, 4);

        _chkSolutionFiles.Text = "Включать файл решения";
        _chkSolutionFiles.AutoSize = true;
        commonGrid.Controls.Add(_chkSolutionFiles, 0, 3);
        commonGrid.SetColumnSpan(_chkSolutionFiles, 2);

        _chkManifest.Text = "Включать _VS.Helper.ZipManifest.txt";
        _chkManifest.AutoSize = true;
        commonGrid.Controls.Add(_chkManifest, 2, 3);
        commonGrid.SetColumnSpan(_chkManifest, 2);
        root.Controls.Add(common, 0, 0);

        SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };
        root.Controls.Add(split, 0, 1);

        GroupBox includeBox = new() { Text = "Что собрать в ZIP — WYSIWYG галочки", Dock = DockStyle.Fill };
        TableLayoutPanel includePanel = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        includePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        includePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        includeBox.Controls.Add(includePanel);

        FlowLayoutPanel buttons = new() { Dock = DockStyle.Top, AutoSize = true };
        AddButton(buttons, "Проекты решения", (_, __) => SelectSolutionProjectFolders());
        AddButton(buttons, "Корневые файлы", (_, __) => SelectRootFiles());
        AddButton(buttons, "Всё", (_, __) => SetAllNodesChecked(true));
        AddButton(buttons, "Ничего", (_, __) => SetAllNodesChecked(false));
        includePanel.Controls.Add(buttons, 0, 0);

        _tree.Dock = DockStyle.Fill;
        _tree.CheckBoxes = true;
        _tree.HideSelection = false;
        _tree.AfterCheck += (_, e) =>
        {
            if (e.Action == TreeViewAction.Unknown)
                return;
            SetChildrenChecked(e.Node, e.Node.Checked);
        };
        includePanel.Controls.Add(_tree, 0, 1);
        split.Panel1.Controls.Add(includeBox);

        GroupBox help = new() { Text = "Подсказка", Dock = DockStyle.Fill };
        Label helpText = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Text = "Когда галочка авто-включения включена, Build Zip сам берёт проекты из решения и ProjectReference.\r\n\r\n" +
                   "Если галочку снять, ZIP собирается по отмеченным здесь файлам и папкам.\r\n\r\n" +
                   "Папка, отмеченная галочкой, сохраняется в config как <File>Папка</File> и пакуется рекурсивно.\r\n\r\n" +
                   "Исключения bin/obj/.vs/.git/_zip и прочий мусор всё равно отсекаются."
        };
        help.Controls.Add(helpText);
        split.Panel2.Controls.Add(help);

        GroupBox excludeBox = new() { Text = "Exclude patterns", Dock = DockStyle.Fill };
        _excludeList.Dock = DockStyle.Fill;
        excludeBox.Controls.Add(_excludeList);
        root.Controls.Add(excludeBox, 0, 2);

        FlowLayoutPanel bottom = new() { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        AddButton(bottom, "Сохранить", (_, __) => SaveAndClose());
        AddButton(bottom, "Отмена", (_, __) => { DialogResult = DialogResult.Cancel; Close(); });
        AddButton(bottom, "Открыть XML", (_, __) => { SaveToConfig(); DialogResult = DialogResult.Retry; Close(); });
        root.Controls.Add(bottom, 0, 3);
    }

    private static void AddLabel(TableLayoutPanel panel, string text, int col, int row)
    {
        panel.Controls.Add(new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 0, 0) }, col, row);
    }

    private static void AddText(TableLayoutPanel panel, TextBox textBox, int col, int row)
    {
        textBox.Dock = DockStyle.Fill;
        panel.Controls.Add(textBox, col, row);
    }

    private static void AddButton(FlowLayoutPanel panel, string text, EventHandler onClick)
    {
        Button button = new() { Text = text, AutoSize = true, Margin = new Padding(4) };
        button.Click += onClick;
        panel.Controls.Add(button);
    }

    private void LoadConfigToUi()
    {
        _txtRoot.Text = _config.Root;
        _txtOutputDir.Text = _config.OutputDir;
        _txtArchiveName.Text = _config.ArchiveName;
        _txtStartProject.Text = _config.StartProject;
        _chkProjectClosure.Checked = _config.IncludeProjectClosure;
        _chkSolutionFiles.Checked = _config.IncludeSolutionFiles;
        _chkManifest.Checked = _config.IncludeManifest;
        _excludeList.Items.Clear();
        foreach (string item in _config.Exclude)
            _excludeList.Items.Add(item);
    }

    private void LoadTree()
    {
        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            HashSet<string> selected = new(_config.Include.Select(Normalize), StringComparer.OrdinalIgnoreCase);

            foreach (string dir in Directory.EnumerateDirectories(_solutionDir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(dir);
                if (IsHiddenOrTrash(name))
                    continue;

                TreeNode node = new(name) { Tag = name };
                node.Checked = selected.Contains(Normalize(name)) || selected.Contains(Normalize(name + "/**"));
                AddChildren(node, dir, name, selected, 0);
                _tree.Nodes.Add(node);
            }

            foreach (string file in Directory.EnumerateFiles(_solutionDir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(file);
                if (IsTrashFile(name))
                    continue;
                TreeNode node = new(name) { Tag = name };
                node.Checked = selected.Contains(Normalize(name));
                _tree.Nodes.Add(node);
            }

            SelectSolutionProjectFolders(false);
            SelectRootFiles(false);
        }
        finally
        {
            _tree.EndUpdate();
        }
    }

    private void AddChildren(TreeNode parent, string fullDir, string relativeDir, HashSet<string> selected, int depth)
    {
        if (depth >= 3)
            return;

        foreach (string dir in Directory.EnumerateDirectories(fullDir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(dir);
            if (IsHiddenOrTrash(name))
                continue;
            string rel = ZipPath.NormalizeRelative(Path.Combine(relativeDir, name));
            TreeNode node = new(name) { Tag = rel };
            node.Checked = selected.Contains(Normalize(rel)) || selected.Contains(Normalize(rel + "/**"));
            AddChildren(node, dir, rel, selected, depth + 1);
            parent.Nodes.Add(node);
        }

        foreach (string file in Directory.EnumerateFiles(fullDir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(file);
            if (IsTrashFile(name))
                continue;
            string rel = ZipPath.NormalizeRelative(Path.Combine(relativeDir, name));
            TreeNode node = new(name) { Tag = rel };
            node.Checked = selected.Contains(Normalize(rel));
            parent.Nodes.Add(node);
        }
    }

    private void SelectSolutionProjectFolders() => SelectSolutionProjectFolders(true);

    private void SelectSolutionProjectFolders(bool clearFirst)
    {
        if (clearFirst)
            SetAllNodesChecked(false);

        string[] folders = SolutionProjectScanner.GetProjects(_solutionPath)
            .Select(x => ZipPath.GetRelativePath(_solutionDir, Path.GetDirectoryName(x) ?? _solutionDir))
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != ".")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string folder in folders)
            CheckNodeByRelative(folder, true);
    }

    private void SelectRootFiles() => SelectRootFiles(true);

    private void SelectRootFiles(bool keepExisting)
    {
        if (!keepExisting)
            return;

        string[] names =
        {
            Path.GetFileName(_solutionPath), ZipBuildConfig.GetFileName(_solutionPath), "README.md", "LICENSE.txt", "LICENSE.md",
            "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json", "NuGet.config",
            ".editorconfig", ".gitignore", ".gitattributes"
        };

        foreach (string name in names)
            CheckNodeByRelative(name, true);
    }

    private void CheckNodeByRelative(string relative, bool isChecked)
    {
        relative = Normalize(relative);
        foreach (TreeNode node in EnumerateNodes(_tree.Nodes))
        {
            if (node.Tag is string tag && Normalize(tag).Equals(relative, StringComparison.OrdinalIgnoreCase))
            {
                node.Checked = isChecked;
                return;
            }
        }
    }

    private void SetAllNodesChecked(bool isChecked)
    {
        foreach (TreeNode node in EnumerateNodes(_tree.Nodes))
            node.Checked = isChecked;
    }

    private static void SetChildrenChecked(TreeNode node, bool isChecked)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = isChecked;
            SetChildrenChecked(child, isChecked);
        }
    }

    private static IEnumerable<TreeNode> EnumerateNodes(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            yield return node;
            foreach (TreeNode child in EnumerateNodes(node.Nodes))
                yield return child;
        }
    }

    private void SaveAndClose()
    {
        SaveToConfig();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SaveToConfig()
    {
        _config.Root = string.IsNullOrWhiteSpace(_txtRoot.Text) ? "." : _txtRoot.Text.Trim();
        _config.OutputDir = string.IsNullOrWhiteSpace(_txtOutputDir.Text) ? "_zip" : _txtOutputDir.Text.Trim();
        _config.ArchiveName = string.IsNullOrWhiteSpace(_txtArchiveName.Text) ? "{Solution.Name}.zip" : _txtArchiveName.Text.Trim();
        _config.StartProject = _txtStartProject.Text.Trim();
        _config.IncludeProjectClosure = _chkProjectClosure.Checked;
        _config.IncludeSolutionFiles = _chkSolutionFiles.Checked;
        _config.IncludeManifest = _chkManifest.Checked;

        _config.Include.Clear();
        foreach (TreeNode node in EnumerateNodes(_tree.Nodes).Where(x => x.Checked))
        {
            if (node.Tag is not string rel || string.IsNullOrWhiteSpace(rel))
                continue;

            string full = Path.Combine(_solutionDir, rel.Replace('/', Path.DirectorySeparatorChar));
            if (Directory.Exists(full))
            {
                bool hasCheckedParent = node.Parent != null && node.Parent.Checked;
                if (!hasCheckedParent)
                    _config.Include.Add(ZipPath.NormalizeRelative(rel));
            }
            else if (File.Exists(full) && (node.Parent == null || !node.Parent.Checked))
            {
                _config.Include.Add(ZipPath.NormalizeRelative(rel));
            }
        }

        _config.Exclude.Clear();
        foreach (object item in _excludeList.Items)
        {
            string value = Convert.ToString(item)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                _config.Exclude.Add(value);
        }

        _config.Save(_solutionPath);
    }

    private static string Normalize(string value) => ZipPath.NormalizeRelative(value ?? string.Empty).TrimEnd('/');

    private static bool IsHiddenOrTrash(string name)
        => name.StartsWith(".", StringComparison.Ordinal) ||
           name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("packages", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("TestResults", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("_zip", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("VSIX", StringComparison.OrdinalIgnoreCase);

    private static bool IsTrashFile(string name)
        => name.EndsWith(".user", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".suo", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".cache", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".vsix", StringComparison.OrdinalIgnoreCase) ||
           name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase);
}
