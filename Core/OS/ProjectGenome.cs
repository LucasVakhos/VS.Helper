using System;
using System.Collections.Generic;

namespace VS.Helper.Core.OS;

public sealed class ProjectGenome
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string SolutionPath { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public int SourceFileCount { get; set; }
    public int ClassCount { get; set; }
    public int InterfaceCount { get; set; }
    public int MethodLikeCount { get; set; }
    public int TodoCount { get; set; }
    public int ApproxLinesOfCode { get; set; }
    public List<string> Projects { get; set; } = new();
    public List<string> HotFiles { get; set; } = new();
    public List<string> TopNamespaces { get; set; } = new();
}
