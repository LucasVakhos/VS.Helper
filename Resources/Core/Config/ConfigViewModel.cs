// <auto-split from VSHelper.Full.cs>
using System;
using System.ComponentModel;
using System.Linq;
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
