using System.Collections.Generic;

namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildConfig
{
    public string Root { get; set; } = "$(SolutionDir)";
    public string OutputDir { get; set; } = "$(SolutionDir)";
    public string ArchiveName { get; set; } = "$(SolutionName)_$(Date)_$(Time).zip";
    public string StartProject { get; set; }
    public bool IncludeManifest { get; set; } = true;
    public bool IncludeProjectClosure { get; set; } = true;
    public List<string> Include { get; } = new List<string>();
    public List<string> Exclude { get; } = new List<string>();

    public static IEnumerable<string> DefaultExcludes()
    {
        yield return "**/bin/**";
        yield return "**/obj/**";
        yield return "**/.vs/**";
        yield return "**/.git/**";
        yield return "**/node_modules/**";
        yield return "**/packages/**";
        yield return "**/*.user";
        yield return "**/*.suo";
        yield return "**/*.pdb";
        yield return "**/*.cache";
        yield return "**/*.log";
        yield return "**/*.db";
        yield return "**/*.sqlite";
        yield return "**/*.zip";
    }
}
