// Commands\CreateCommentCommand.cs
using EnvDTE;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VS.Helper.Commands;

[Command(PackageIds.CreateCommentCommand)]
internal sealed class CreateCommentCommand : BaseCommand<CreateCommentCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        string comment = await CreateCommentAsync();

        if (!IsFocusInCodeEditor())
        {
            System.Windows.Forms.MessageBox.Show(
                "Команду \"Вставить комментарий\" нужно выполнять из окна редактора кода.",
                "VS.Helper",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            return;
        }

        InsertIntoActiveDocument(comment);
    }

    private static void InsertIntoActiveDocument(string comment)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        DTE dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
        if (dte == null)
            return;

        if (!(dte.ActiveDocument != null && dte.ActiveDocument.Object("TextDocument") is TextDocument textDocument))
            return;

        EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
        editPoint.Insert(comment + Environment.NewLine);
    }

    private static async Task<string> CreateCommentAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        DTE dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;

        string filePath = null;
        string solutionPath = null;

        if (dte != null)
        {
            if (dte.ActiveDocument != null)
                filePath = dte.ActiveDocument.FullName;

            if (dte.Solution != null)
                solutionPath = dte.Solution.FullName;
        }

        if (string.IsNullOrWhiteSpace(filePath))
            return "//";

        string relativePath = GetRelativePath(solutionPath, filePath)
            .Replace("/", "\\");

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        switch (extension)
        {
            case ".razor":
            case ".cshtml":
                return "@* " + relativePath + " *@";

            case ".html":
            case ".xml":
            case ".xaml":
                return "<!-- " + relativePath + " -->";

            case ".css":
            case ".scss":
            case ".less":
                return "/* " + relativePath + " */";

            case ".sql":
                return "-- " + relativePath;

            case ".vb":
                return "' " + relativePath;

            case ".ps1":
                return "# " + relativePath;

            default:
                return "// " + relativePath;
        }
    }

    private static bool IsFocusInCodeEditor()
    {
        IntPtr hwnd = GetFocus();

        if (hwnd == IntPtr.Zero)
            return false;

        string className = GetWindowClassName(hwnd);

        return className.IndexOf("WpfTextView", StringComparison.OrdinalIgnoreCase) >= 0
            || className.IndexOf("VsTextEditPane", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        StringBuilder className = new StringBuilder(256);
        GetClassName(hwnd, className, 256);
        return className.ToString();
    }

    private static string GetRelativePath(string solutionPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return Path.GetFileName(filePath);

        string solutionDir = Path.GetDirectoryName(solutionPath);

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
