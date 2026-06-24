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

internal sealed class SwarmError
{
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }

    public string Fingerprint => SwarmHash.Normalize(Description);
}
