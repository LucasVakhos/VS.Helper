using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using VS.Helper.Core;

public partial class ZipToolWindowControl : System.Windows.Controls.UserControl
{
    private bool _outputTouchedByUser;
    private bool _updatingOutputInternally;

    public ZipToolWindowControl()
    {
        InitializeComponent();
        SeedDefaults();
    }

    private void Build(object sender, RoutedEventArgs e)
    {
        string sourcePath = (SourcePathBox.Text ?? string.Empty).Trim();
        string outputZip = (OutputZipBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
        {
            SetStatus("Source folder не найден.");
            System.Windows.MessageBox.Show("Укажи существующую папку Source Folder.", "VS.Helper ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputZip))
        {
            SetStatus("Output ZIP не указан.");
            System.Windows.MessageBox.Show("Укажи путь к выходному ZIP-файлу.", "VS.Helper ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!outputZip.EndsWith(".zip", System.StringComparison.OrdinalIgnoreCase))
            outputZip += ".zip";

        try
        {
            string outputDir = Path.GetDirectoryName(outputZip);
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                SetStatus("Некорректный путь Output ZIP.");
                System.Windows.MessageBox.Show("Некорректный путь к ZIP-файлу.", "VS.Helper ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Directory.CreateDirectory(outputDir);
            if (File.Exists(outputZip))
                File.Delete(outputZip);

            // Get optional Access Key
            string accessKey = (AccessKeyBox.Text ?? string.Empty).Trim();

            SetStatus("Building ZIP...");
            var zip = new ZipEngine();
            zip.Build(sourcePath, outputZip);
            
            if (!string.IsNullOrWhiteSpace(accessKey))
            {
                // Store access key for later use (e.g., cloud sync)
                SetStatus($"Done: {outputZip} (access key set)");
            }
            else
            {
                SetStatus("Done: " + outputZip);
            }

            System.Windows.MessageBox.Show("ZIP успешно создан:\n" + outputZip, "VS.Helper ZIP", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            SetStatus("Build failed.");
            System.Windows.MessageBox.Show(ex.Message, "VS.Helper ZIP / Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenOutputFolder(object sender, RoutedEventArgs e)
    {
        string outputZip = (OutputZipBox.Text ?? string.Empty).Trim();
        string outputDir = Path.GetDirectoryName(outputZip);

        if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
        {
            System.Windows.MessageBox.Show("Папка вывода не найдена.", "VS.Helper ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = outputDir,
            UseShellExecute = true
        });

        SetStatus("Opened: " + outputDir);
    }

    private void BrowseSource(object sender, RoutedEventArgs e)
    {
        using (var dialog = new WinForms.FolderBrowserDialog())
        {
            string current = (SourcePathBox.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                dialog.SelectedPath = current;

            dialog.Description = "Выбери исходную папку для ZIP";

            if (dialog.ShowDialog() == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SourcePathBox.Text = dialog.SelectedPath;
                SetStatus("Source selected");
            }
        }
    }

    private void BrowseOutput(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Выбери выходной ZIP файл",
            Filter = "ZIP archive (*.zip)|*.zip|All files (*.*)|*.*",
            DefaultExt = ".zip",
            AddExtension = true,
            OverwritePrompt = false
        };

        string current = (OutputZipBox.Text ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(current))
        {
            try
            {
                string dir = Path.GetDirectoryName(current);
                string file = Path.GetFileName(current);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    dialog.InitialDirectory = dir;
                if (!string.IsNullOrWhiteSpace(file))
                    dialog.FileName = file;
            }
            catch
            {
                // Ignore invalid path fragments and let dialog use defaults.
            }
        }

        if (dialog.ShowDialog() == true)
        {
            OutputZipBox.Text = dialog.FileName;
            SetStatus("Output selected");
        }
    }

    private void SourcePathChanged(object sender, TextChangedEventArgs e)
    {
        UpdateOutputFromSourceIfAuto();
    }

    private void OutputZipChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingOutputInternally)
            return;

        _outputTouchedByUser = !string.IsNullOrWhiteSpace((OutputZipBox.Text ?? string.Empty).Trim());
    }

    private void PathBoxDragOver(object sender, DragEventArgs e)
    {
        if (TryGetDroppedDirectory(e.Data, out _))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void SourcePathDrop(object sender, DragEventArgs e)
    {
        if (!TryGetDroppedDirectory(e.Data, out string droppedDir))
        {
            SetStatus("Drop a folder to Source.");
            return;
        }

        SourcePathBox.Text = droppedDir;
        SetStatus("Source dropped");
    }

    private static bool TryGetDroppedDirectory(IDataObject dataObject, out string directory)
    {
        directory = string.Empty;

        if (dataObject == null || !dataObject.GetDataPresent(DataFormats.FileDrop))
            return false;

        string[] items = dataObject.GetData(DataFormats.FileDrop) as string[];
        if (items == null || items.Length == 0)
            return false;

        string candidate = items[0];
        if (Directory.Exists(candidate))
        {
            directory = candidate;
            return true;
        }

        if (File.Exists(candidate))
        {
            string parent = Path.GetDirectoryName(candidate);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                directory = parent;
                return true;
            }
        }

        return false;
    }

    private void UpdateOutputFromSourceIfAuto()
    {
        if (_outputTouchedByUser)
            return;

        string sourcePath = (SourcePathBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            return;

        string sourceName = new DirectoryInfo(sourcePath).Name;
        if (string.IsNullOrWhiteSpace(sourceName))
            sourceName = "archive";

        string parent = Directory.GetParent(sourcePath)?.FullName;
        if (string.IsNullOrWhiteSpace(parent))
            parent = sourcePath;

        string suggested = Path.Combine(parent, "_zip", sourceName + ".zip");

        _updatingOutputInternally = true;
        try
        {
            OutputZipBox.Text = suggested;
        }
        finally
        {
            _updatingOutputInternally = false;
        }
    }

    private void SeedDefaults()
    {
        string current = Directory.GetCurrentDirectory();
        SourcePathBox.Text = current;
        _outputTouchedByUser = false;
        UpdateOutputFromSourceIfAuto();
        SetStatus("Ready");
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    public string RunSelfTest()
    {
        StringBuilder sb = new StringBuilder();
        bool ok = true;

        try
        {
            string current = Directory.GetCurrentDirectory();
            sb.AppendLine("CurrentDir: " + current);

            bool sourceExists = Directory.Exists(SourcePathBox.Text);
            sb.AppendLine("SourceExists: " + sourceExists);
            ok &= sourceExists;

            bool outputHasZipExt = (OutputZipBox.Text ?? string.Empty).EndsWith(".zip", System.StringComparison.OrdinalIgnoreCase);
            sb.AppendLine("OutputEndsWithZip: " + outputHasZipExt);
            ok &= outputHasZipExt;

            DataObject data = new DataObject(DataFormats.FileDrop, new[] { current });
            bool dropParsed = TryGetDroppedDirectory(data, out string dropped);
            sb.AppendLine("DropParsed: " + dropParsed);
            sb.AppendLine("DroppedPath: " + dropped);
            ok &= dropParsed && string.Equals(dropped, current, System.StringComparison.OrdinalIgnoreCase);

            string previousOutput = OutputZipBox.Text;
            _outputTouchedByUser = false;
            SourcePathBox.Text = current;
            UpdateOutputFromSourceIfAuto();
            bool outputAutoFilled = !string.IsNullOrWhiteSpace(OutputZipBox.Text) && OutputZipBox.Text.EndsWith(".zip", System.StringComparison.OrdinalIgnoreCase);
            sb.AppendLine("OutputAutoFilled: " + outputAutoFilled);
            ok &= outputAutoFilled;

            OutputZipBox.Text = previousOutput;
            sb.AppendLine("SelfTest: " + (ok ? "PASS" : "FAIL"));
            return sb.ToString();
        }
        catch (System.Exception ex)
        {
            sb.AppendLine("SelfTest: FAIL");
            sb.AppendLine(ex.ToString());
            return sb.ToString();
        }
    }
}