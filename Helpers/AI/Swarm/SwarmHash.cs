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

internal static class SwarmHash
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "EMPTY";

        string value = Regex.Replace(text, "\\s+", " ").Trim();
        value = Regex.Replace(value, "\\d+", "#");
        return value.Length <= 160 ? value : value.Substring(0, 160);
    }
}
