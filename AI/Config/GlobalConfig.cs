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

internal sealed class GlobalConfig
{
    public string AccessKey { get; set; } = string.Empty;
    public string DefaultBrowser { get; set; } = string.Empty;
    public bool AiRouterEnabled { get; set; } = true;
    public bool SmartRoutingEnabled { get; set; } = true;
    public string Version { get; set; } = "1.0.0";
}
