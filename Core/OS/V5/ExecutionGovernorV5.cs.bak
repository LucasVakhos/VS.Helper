using System;
using System.Threading.Tasks;

namespace VS.Helper.Core.OS.V5;

public class ExecutionGovernorV5
{
    private static readonly string[] _blockedKeywords = { "rm -rf", "format c:", "del /s", "shutdown", "freeze" };

    private readonly EventBus _bus;
    private readonly PersistentMemory _memory;

    public ExecutionGovernorV5(EventBus bus, PersistentMemory memory)
    {
        _bus = bus;
        _memory = memory;
    }

    public Task<string> ExecuteAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult("BLOCKED:empty");

        _bus.Emit("governor.check", input);

        foreach (string blocked in _blockedKeywords)
        {
            if (input.IndexOf(blocked, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _bus.Emit("governor.blocked", blocked);
                return Task.FromResult("BLOCKED:" + blocked);
            }
        }

        string result = "OK:" + input;
        _bus.Emit("governor.done", result);
        return Task.FromResult(result);
    }
}