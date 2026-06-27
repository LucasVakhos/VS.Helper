using System;
using System.IO;

namespace VS.Helper.Core.OS;

public sealed class EngineContext
{
    public EngineContext(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            throw new ArgumentException("Solution path is empty.", nameof(solutionPath));

        SolutionPath = solutionPath;
        SolutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        WorkDirectory = Path.Combine(SolutionDirectory, ".vshelper");
        Directory.CreateDirectory(WorkDirectory);
    }

    public string SolutionPath { get; }
    public string SolutionDirectory { get; }
    public string WorkDirectory { get; }
}
