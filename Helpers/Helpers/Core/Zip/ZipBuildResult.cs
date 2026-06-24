using System.Collections.Generic;

namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildResult
{
    public ZipBuildResult(string zipPath, string configPath, IReadOnlyList<string> includedFiles)
    {
        ZipPath = zipPath;
        ConfigPath = configPath;
        IncludedFiles = includedFiles;
    }

    public string ZipPath { get; }
    public string ConfigPath { get; }
    public IReadOnlyList<string> IncludedFiles { get; }
}
