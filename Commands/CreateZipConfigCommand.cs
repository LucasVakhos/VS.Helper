using System;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace VS.Helper.Commands;

internal sealed class CreateZipConfigCommand : BaseCommand<CreateZipConfigCommand>
{
    public static async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) as DTE2;
        if (dte?.Solution == null || string.IsNullOrWhiteSpace(dte.Solution.FullName))
            return;

        var slnPath = dte.Solution.FullName;
        var slnDir = Path.GetDirectoryName(slnPath);
        if (string.IsNullOrWhiteSpace(slnDir))
            return;

        var project = Path.GetFileNameWithoutExtension(slnPath);
        var zipPath = Path.Combine(slnDir, $"{project}.zip");

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(
            slnDir,
            zipPath,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);

        var files = new StringCollection { zipPath };
        var data = new System.Windows.Forms.DataObject();
        data.SetFileDropList(files);
        System.Windows.Forms.Clipboard.SetDataObject(data, copy: true);

        System.Windows.Forms.MessageBox.Show(
            $"'{project}.sln' скопирован в '{project}.zip'",
            "Готово",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
    }
}
