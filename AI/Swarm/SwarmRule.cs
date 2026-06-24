// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using SysProcess = System.Diagnostics.Process;
namespace VS.Helper.AI;

internal sealed class SwarmRule
{
    public string ErrorPattern { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public int SuccessScore { get; set; }
    public int FailCount { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
}
