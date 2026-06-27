// <auto-split from VSHelper.Full.cs>
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Windows.Controls;
using Process = System.Diagnostics.Process;
namespace VS.Helper;

internal sealed class BuildSolutionCommand
{
    public static async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        if (dte?.Solution == null) return;

        foreach (EnvDTE.Project project in dte.Solution.Projects)
        {
            try
            {
                project.Properties?.Item("Version")?.Value?.ToString();
            }
            catch { }
        }

        dte.Solution.SolutionBuild.Build(true);
    }
}
