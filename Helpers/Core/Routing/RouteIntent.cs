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

internal class RouteIntent
{
    public RouteTarget Target { get; set; }
    public string Payload { get; set; }
    public string Title { get; set; }
}
