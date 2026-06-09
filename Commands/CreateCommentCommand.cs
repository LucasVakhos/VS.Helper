// Commands\CreateCommentCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace VS.Helper.Commands;

[Command(PackageIds.CreateCommentCommand)]
internal sealed class CreateCommentCommand : BaseCommand<CreateCommentCommand>
{
    private static WindowEvents? _windowEvents;
    private static bool _isAutoRunning;
    private static DateTime _lastAutoRun = DateTime.MinValue;

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await RunAsync();
    }

    internal static async Task StartGitChangesAutoPasteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (Package.GetGlobalService(typeof(DTE)) is not DTE dte)
            return;

        _windowEvents = dte.Events.WindowEvents;

        _windowEvents.WindowActivated += async (gotFocus, lostFocus) =>
        {
            await OnWindowActivatedAsync(gotFocus);
        };
    }

    private static async Task OnWindowActivatedAsync(Window gotFocus)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (_isAutoRunning)
            return;

        if ((DateTime.Now - _lastAutoRun).TotalSeconds < 2)
            return;

        string caption = gotFocus.Caption ?? string.Empty;

        bool isGitChanges =
            caption.Contains("Git Changes", StringComparison.OrdinalIgnoreCase) ||
            caption.Contains("Изменения Git", StringComparison.OrdinalIgnoreCase);

        if (!isGitChanges)
            return;

        _isAutoRunning = true;
        _lastAutoRun = DateTime.Now;

        try
        {
            await Task.Delay(300);

            string stamp = await CreateStampAsync();

            await PasteIntoFocusedControlAsync(stamp);
        }
        finally
        {
            _isAutoRunning = false;
        }
    }

    private static async Task RunAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        string stamp = await CreateStampAsync();

        if (IsFocusInCodeEditor())
        {
            InsertIntoActiveDocument(stamp);
            return;
        }

        await PasteIntoFocusedControlAsync(stamp);
    }

    private static void InsertIntoActiveDocument(string stamp)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (Package.GetGlobalService(typeof(DTE)) is not DTE dte)
            return;

        if (dte.ActiveDocument?.Object("TextDocument") is not TextDocument textDocument)
            return;

        EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
        editPoint.Insert(stamp + Environment.NewLine);
    }

    private static async Task PasteIntoFocusedControlAsync(string stamp)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        Clipboard.SetText(stamp);

        await Task.Delay(150);

        SendKeys.SendWait("^v");
    }

    private static async Task<string> CreateStampAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        DTE? dte = Package.GetGlobalService(typeof(DTE)) as DTE;

        string? filePath = dte?.ActiveDocument?.FullName;
        string? solutionPath = dte?.Solution?.FullName;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            string solutionName = GetSolutionName(solutionPath);
            return $"{solutionName} {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        string relativePath = GetRelativePath(solutionPath, filePath)
            .Replace("/", "\\");

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".razor" => $"@* {relativePath} *@",
            ".cshtml" => $"@* {relativePath} *@",

            ".html" => $"<!-- {relativePath} -->",
            ".xml" => $"<!-- {relativePath} -->",
            ".xaml" => $"<!-- {relativePath} -->",

            ".css" => $"/* {relativePath} */",
            ".scss" => $"/* {relativePath} */",
            ".less" => $"/* {relativePath} */",

            ".sql" => $"-- {relativePath}",
            ".vb" => $"'{relativePath}",
            ".ps1" => $"# {relativePath}",

            _ => $"// {relativePath}"
        };
    }

    private static bool IsFocusInCodeEditor()
    {
        IntPtr hwnd = GetFocus();

        if (hwnd == IntPtr.Zero)
            return false;

        string className = GetWindowClassName(hwnd);

        return className.Contains("WpfTextView", StringComparison.OrdinalIgnoreCase)
            || className.Contains("VsTextEditPane", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        StringBuilder className = new(256);
        GetClassName(hwnd, className, 256);
        return className.ToString();
    }

    private static string GetSolutionName(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return "Project";

        string? name = Path.GetFileNameWithoutExtension(solutionPath);

        return string.IsNullOrWhiteSpace(name) ? "Project" : name;
    }

    private static string GetRelativePath(string? solutionPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return Path.GetFileName(filePath);

        string? solutionDir = Path.GetDirectoryName(solutionPath);

        if (string.IsNullOrWhiteSpace(solutionDir))
            return Path.GetFileName(filePath);

        Uri baseUri = new Uri(AppendDirectorySeparatorChar(solutionDir));
        Uri fileUri = new Uri(filePath);

        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(
        IntPtr hWnd,
        StringBuilder lpClassName,
        int nMaxCount);
}