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

internal class ConfigViewModel : INotifyPropertyChanged
{
    private GlobalConfig cfg = GlobalConfigStore.Load();

    public string AccessKey
    {
        get => cfg.AccessKey;
        set { cfg.AccessKey = value; Save(); }
    }

    public string DefaultBrowser
    {
        get => cfg.DefaultBrowser;
        set { cfg.DefaultBrowser = value; Save(); }
    }

    public bool AiRouterEnabled
    {
        get => cfg.AiRouterEnabled;
        set { cfg.AiRouterEnabled = value; Save(); }
    }

    public bool SmartRoutingEnabled
    {
        get => cfg.SmartRoutingEnabled;
        set { cfg.SmartRoutingEnabled = value; Save(); }
    }

    private void Save()
    {
        GlobalConfigStore.Save(cfg);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}

