// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace VS.Helper.AI;

internal static class ThreadOrchestrator
{
    private static readonly SemaphoreSlim UiGate = new(1, 1);
    private static readonly SemaphoreSlim BgGate = new(1, 1);

    public static async Task RunUIAsync(Func<Task> action)
    {
        await UiGate.WaitAsync();
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await action();
        }
        finally
        {
            UiGate.Release();
        }
    }

    public static async Task<T> RunUIAsync<T>(Func<Task<T>> action)
    {
        T result = default!;
        await RunUIAsync(async () => result = await action());
        return result;
    }

    public static async Task RunBackgroundAsync(Func<Task> action)
    {
        await BgGate.WaitAsync();
        try
        {
            await Task.Run(action);
        }
        finally
        {
            BgGate.Release();
        }
    }
}
