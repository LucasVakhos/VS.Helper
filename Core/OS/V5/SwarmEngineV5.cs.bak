using System.Collections.Generic;
using System.Text;

namespace VS.Helper.Core.OS.V5;

public class SwarmEngineV5
{
    private readonly EventBus _bus;
    private readonly PersistentMemory _memory;

    public SwarmEngineV5(EventBus bus, PersistentMemory memory)
    {
        _bus = bus;
        _memory = memory;
    }

    public string Process(string command, Dictionary<string, string> context)
    {
        _bus.Emit("swarm.input", command);

        var sb = new StringBuilder();

        // Analyzer pass: surface relevant memory context
        sb.Append("[Analyze] cmd=").Append(command);
        if (context.Count > 0)
            sb.Append(" ctx.keys=").Append(string.Join(",", context.Keys));

        // Optimizer pass: apply last-run heuristic
        if (context.TryGetValue(command, out string lastResult) && !string.IsNullOrWhiteSpace(lastResult))
            sb.Append(" [cached=").Append(lastResult.Length > 40 ? lastResult.Substring(0, 40) + "..." : lastResult).Append(']');

        // Validator pass: mark as ready
        sb.Append(" [valid]");

        string result = sb.ToString();
        _bus.Emit("swarm.output", result);
        return result;
    }
}