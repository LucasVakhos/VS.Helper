// <auto-split from VSHelper.Full.cs>
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Windows.Controls;
using Process = System.Diagnostics.Process;
namespace VS.Helper;

internal class GlobalConfig
{
    public string AccessKey { get; set; } = "";
    public string DefaultBrowser { get; set; } = "";
    public bool AiRouterEnabled { get; set; } = true;
    public bool SmartRoutingEnabled { get; set; } = true;
    public string Version { get; set; } = "1.0.0";
}
