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

internal static class SwarmStrategies
{
    public const string AddAlias = "ADD_ALIAS";
    public const string Rebuild = "REBUILD";
    public const string ThreadOrchestratorHint = "THREAD_ORCHESTRATOR_HINT";
    public const string SafeMode = "SAFE_MODE";
}
