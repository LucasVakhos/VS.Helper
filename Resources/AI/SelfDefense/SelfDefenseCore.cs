// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
namespace VS.Helper.AI;

internal static class SelfDefenseCore
{
    public static bool CheckSystem()
    {
        return CheckConfig() && CheckRuntimeState();
    }

    private static bool CheckConfig()
    {
        try
        {
            return GlobalConfigStore.Load() != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckRuntimeState()
    {
        return AppDomain.CurrentDomain.FriendlyName.IndexOf("devenv", StringComparison.OrdinalIgnoreCase) >= 0
            || AppDomain.CurrentDomain.FriendlyName.IndexOf("DefaultDomain", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
