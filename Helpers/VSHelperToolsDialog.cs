// Helpers\VSHelperToolsDialog.cs
// Commands\VSHelperToolsDialog.cs
using System.IO;
using System.Windows.Forms;

namespace VS.Helper.Commands;

internal sealed class VSHelperToolsDialog : Form
{
    private readonly VSHelperSolutionInfo _solution;
    private readonly ComboBox _combo = new ComboBox();
    private readonly TextBox _solutionPath = new TextBox();
    private readonly ComboBox _projectPath = new ComboBox();
    private readonly ComboBox _sampleProjectPath = new ComboBox();
    private readonly TextBox _placePath = new TextBox();
    private readonly TextBox _findText = new TextBox();
    private readonly TextBox _replaceText = new TextBox();
    private readonly ComboBox _pattern = new ComboBox();
    private readonly CheckBox _backup = new CheckBox();
    private readonly CheckBox _dryRun = new CheckBox();

    private readonly Label _solutionLabel = new Label();
    private readonly Label _projectLabel = new Label();
    private readonly Label _sampleProjectLabel = new Label();
    private readonly Label _placeLabel = new Label();
    private readonly Label _findLabel = new Label();
    private readonly Label _replaceLabel = new Label();
    private readonly Label _patternLabel = new Label();

    private readonly Button _placeBrowse = new Button();

    public VSHelperToolsDialog(VSHelperSolutionInfo solution)
    {
        _solution = solution;

        Text = "VS.Helper / VSHelper Tools";
        Width = 800;
        Height = 430;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _combo.DropDownStyle = ComboBoxStyle.DropDownList;
        _combo.Left = 16;
        _combo.Top = 16;
        _combo.Width = 748;

        foreach (VSHelperComboTodoItems item in Enum.GetValues(typeof(VSHelperComboTodoItems)))
            _combo.Items.Add(new ComboItem(item));

        _combo.SelectedIndex = 0;
        _combo.SelectedIndexChanged += delegate { ApplySelectedMetadata(); };

        _solutionLabel.Left = 16;
        _solutionLabel.Top = 58;
        _solutionLabel.Width = 180;
        _solutionLabel.Text = "Открытый solution:";

        _solutionPath.Left = 200;
        _solutionPath.Top = 55;
        _solutionPath.Width = 564;
        _solutionPath.ReadOnly = true;
        _solutionPath.Text = solution.SolutionPath;

        _projectLabel.Left = 16;
        _projectLabel.Top = 92;
        _projectLabel.Width = 180;

        _projectPath.Left = 200;
        _projectPath.Top = 89;
        _projectPath.Width = 564;
        _projectPath.DropDownStyle = ComboBoxStyle.DropDownList;

        _sampleProjectLabel.Left = 16;
        _sampleProjectLabel.Top = 126;
        _sampleProjectLabel.Width = 180;

        _sampleProjectPath.Left = 200;
        _sampleProjectPath.Top = 123;
        _sampleProjectPath.Width = 564;
        _sampleProjectPath.DropDownStyle = ComboBoxStyle.DropDownList;

        _placeLabel.Left = 16;
        _placeLabel.Top = 160;
        _placeLabel.Width = 180;

        _placePath.Left = 200;
        _placePath.Top = 157;
        _placePath.Width = 482;
        _placePath.Text = solution.SolutionDir;

        _placeBrowse.Left = 688;
        _placeBrowse.Top = 155;
        _placeBrowse.Width = 76;
        _placeBrowse.Text = "...";
        _placeBrowse.Click += delegate { BrowsePath(_placePath); };

        _findLabel.Left = 16;
        _findLabel.Top = 194;
        _findLabel.Width = 180;
        _findLabel.Text = "Найти:";

        _findText.Left = 200;
        _findText.Top = 191;
        _findText.Width = 564;

        _replaceLabel.Left = 16;
        _replaceLabel.Top = 228;
        _replaceLabel.Width = 180;
        _replaceLabel.Text = "Заменить на:";

        _replaceText.Left = 200;
        _replaceText.Top = 225;
        _replaceText.Width = 564;

        _patternLabel.Left = 16;
        _patternLabel.Top = 263;
        _patternLabel.Width = 180;
        _patternLabel.Text = "Маска:";

        _pattern.Left = 200;
        _pattern.Top = 260;
        _pattern.Width = 120;
        _pattern.DropDownStyle = ComboBoxStyle.DropDownList;
        _pattern.Items.AddRange(new object[] { "*.cs", "*.txt", "*.razor", "*.bak", "*.*" });
        _pattern.SelectedIndex = 0;

        _backup.Left = 350;
        _backup.Top = 262;
        _backup.Width = 170;
        _backup.Text = "Создавать .bak";

        _dryRun.Left = 530;
        _dryRun.Top = 262;
        _dryRun.Width = 170;
        _dryRun.Text = "Dry Run";

        TextBox hint = new TextBox();
        hint.Left = 16;
        hint.Top = 296;
        hint.Width = 748;
        hint.Height = 52;
        hint.Multiline = true;
        hint.ReadOnly = true;
        hint.ScrollBars = ScrollBars.Vertical;
        hint.Text =
            "Работа идёт внутри уже открытого .sln/.slnx. Для обычных операций сканируется папка solution. " +
            "Поля Find/Replace, Project/Sample/Place показываются только когда нужны выбранной операции.";

        Button ok = new Button();
        ok.Left = 606;
        ok.Top = 358;
        ok.Width = 75;
        ok.Text = "Старт";
        ok.DialogResult = DialogResult.OK;

        Button cancel = new Button();
        cancel.Left = 689;
        cancel.Top = 358;
        cancel.Width = 75;
        cancel.Text = "Отмена";
        cancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            _combo,
            _solutionLabel, _solutionPath,
            _projectLabel, _projectPath,
            _sampleProjectLabel, _sampleProjectPath,
            _placeLabel, _placePath, _placeBrowse,
            _findLabel, _findText,
            _replaceLabel, _replaceText,
            _patternLabel, _pattern,
            _backup, _dryRun,
            hint,
            ok, cancel
        });

        LoadProjects();

        AcceptButton = ok;
        CancelButton = cancel;

        ApplySelectedMetadata();
    }

    public VSHelperOptions GetOptions()
    {
        ComboItem selected = (ComboItem)_combo.SelectedItem;
        VSHelperComboTodoAttribute attr = selected.Value.GetAttribute<VSHelperComboTodoAttribute>();

        string searchPath = _solution.SolutionDir;

        if (attr.ShowProject && _projectPath.SelectedItem != null)
            searchPath = Convert.ToString(_projectPath.SelectedItem);

        string placePath = _solution.SolutionDir;

        if (attr.ShowSampleProject && _sampleProjectPath.SelectedItem != null)
            placePath = Convert.ToString(_sampleProjectPath.SelectedItem);
        else if (attr.ShowPlace)
            placePath = _placePath.Text.Trim();

        return new VSHelperOptions
        {
            Item = selected.Value,
            SolutionPath = _solution.SolutionPath,
            SolutionDir = _solution.SolutionDir,
            SearchPath = searchPath,
            PlacePath = placePath,
            FindText = _findText.Text,
            ReplaceText = _replaceText.Text,
            Pattern = Convert.ToString(_pattern.SelectedItem),
            UseBackup = _backup.Checked,
            DryRun = _dryRun.Checked
        };
    }

    private void LoadProjects()
    {
        string[] projects = VSHelperToolsHelper.GetProjectsFromSolution(_solution);

        foreach (string project in projects)
        {
            _projectPath.Items.Add(project);
            _sampleProjectPath.Items.Add(project);
        }

        if (_projectPath.Items.Count > 0)
            _projectPath.SelectedIndex = 0;

        if (_sampleProjectPath.Items.Count > 0)
            _sampleProjectPath.SelectedIndex = 0;
    }

    private void ApplySelectedMetadata()
    {
        ComboItem selected = _combo.SelectedItem as ComboItem;
        if (selected == null)
            return;

        VSHelperComboTodoAttribute attr = selected.Value.GetAttribute<VSHelperComboTodoAttribute>();

        _projectLabel.Text = attr.SearchLabel;
        _sampleProjectLabel.Text = attr.PlaceLabel;
        _placeLabel.Text = attr.PlaceLabel;

        _projectLabel.Visible = attr.ShowProject;
        _projectPath.Visible = attr.ShowProject;

        _sampleProjectLabel.Visible = attr.ShowSampleProject;
        _sampleProjectPath.Visible = attr.ShowSampleProject;

        _placeLabel.Visible = attr.ShowPlace && !attr.ShowSampleProject;
        _placePath.Visible = attr.ShowPlace && !attr.ShowSampleProject;
        _placeBrowse.Visible = attr.ShowPlace && !attr.ShowSampleProject;

        _findLabel.Visible = attr.ShowFind;
        _findText.Visible = attr.ShowFind;

        _replaceLabel.Visible = attr.ShowReplace;
        _replaceText.Visible = attr.ShowReplace;

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
            using OpenFileDialog dialog = new OpenFileDialog();
            dialog.FileName = current;
            dialog.Filter = "All files|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
                textBox.Text = dialog.FileName;

            return;
        }

        using FolderBrowserDialog folder = new FolderBrowserDialog();
        if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
            folder.SelectedPath = current;

        if (folder.ShowDialog() == DialogResult.OK)
            textBox.Text = folder.SelectedPath;
    }

    private sealed class ComboItem
    {
        public ComboItem(VSHelperComboTodoItems value)
        {
            Value = value;
        }

        public VSHelperComboTodoItems Value { get; }

        public override string ToString()
        {
            VSHelperComboTodoAttribute attr = Value.GetAttribute<VSHelperComboTodoAttribute>();
            return attr == null ? Value.ToString() : attr.Name;
        }
    }
}
