namespace VS.Helper.Core.OS.V5;

public class CoreEngineV5
{
    private readonly PersistentMemory _memory = new();
    private readonly EventBus _bus = new();
    private readonly SwarmEngineV5 _swarm;
    private readonly ExecutionGovernorV5 _governor;

    public CoreEngineV5()
    {
        _swarm = new SwarmEngineV5(_bus, _memory);
        _governor = new ExecutionGovernorV5(_bus, _memory);
    }

    public async Task<string> ExecuteAsync(string command)
    {
        _bus.Emit("core.start", command);

        var context = _memory.Load();
        var swarmResult = _swarm.Process(command, context);
        var result = await _governor.ExecuteAsync(swarmResult);

        _memory.Save(command, result);

        _bus.Emit("core.end", result);

        return result;
    }
}
