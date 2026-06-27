// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
namespace VS.Helper.AI;

internal static class DefenseResponse
{
    public static void SafeModeActivate()
    {
        var cfg = GlobalConfigStore.Load();
        cfg.AiRouterEnabled = false;
        cfg.SmartRoutingEnabled = false;
        GlobalConfigStore.Save(cfg);
    }
}
