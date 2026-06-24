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

internal sealed class SwarmMemoryEntry
{
    public string Fingerprint { get; set; } = string.Empty;
    public string LastError { get; set; } = string.Empty;
    public string LastStrategy { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}
