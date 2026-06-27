// <auto-split from VSHelper.AgentSwarm.Full.cs>
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace VS.Helper.AI;

#pragma warning disable VSTHRD010
internal static class DTEProxy
{
    public static Task<DTE2?> GetDteAsync()
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            return Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) as DTE2;
        });
    }

    public static Task<string> GetSolutionPathAsync(DTE2 dte)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            return dte.Solution?.FullName ?? string.Empty;
        });
    }

    public static Task<List<SwarmError>> GetErrorsAsync(DTE2 dte)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            var result = new List<SwarmError>();
            var items = dte.ToolWindows.ErrorList.ErrorItems;

            for (int i = 1; i <= items.Count; i++)
            {
                var item = items.Item(i);
                result.Add(new SwarmError
                {
                    Description = item.Description ?? string.Empty,
                    FileName = item.FileName ?? string.Empty,
                    Line = item.Line,
                    Column = item.Column
                });
            }

            return result;
        });
    }

    public static Task RebuildSolutionAsync(DTE2 dte)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            dte.ExecuteCommand("Build.RebuildSolution");
        });
    }

    public static Task BuildSolutionAsync(DTE2 dte)
    {
        return BuildSolutionNoWaitAsync(dte);
    }

    public static Task BuildSolutionNoWaitAsync(DTE2 dte)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            // false = не ждать окончания сборки; иначе команда расширения может намертво подвесить Visual Studio.
            dte.Solution.SolutionBuild.Build(false);
        });
    }

    public static Task SaveAllAsync(DTE2 dte)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            try
            {
                dte.ExecuteCommand("File.SaveAll");
            }
            catch
            {
                // save-all is best-effort only
            }
        });
    }

    public static Task SetStatusAsync(DTE2 dte, string text)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            dte.StatusBar.Text = text;
        });
    }

    public static Task InsertAtTopOfActiveDocumentAsync(DTE2 dte, string text)
    {
        return ThreadOrchestrator.RunUIAsync(async () =>
        {
            await Task.CompletedTask;
            if (dte.ActiveDocument?.Object("TextDocument") is not TextDocument textDocument)
                return;

            var edit = textDocument.StartPoint.CreateEditPoint();
            edit.Insert(text + Environment.NewLine);
        });
    }
}
#pragma warning restore VSTHRD010
