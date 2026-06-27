using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VS.Helper.Core.OS;

public sealed class EngineBus
{
    private readonly Dictionary<string, IEngineCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public EngineBus Register(IEngineCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        _commands[command.Name] = command;
        return this;
    }

    public async Task<EngineCommandResult> ExecuteAsync(string commandName, EngineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return EngineCommandResult.Fail("unknown", "Command name is empty.");

        if (!_commands.TryGetValue(commandName, out IEngineCommand command))
            return EngineCommandResult.Fail(commandName, "Engine command is not registered.");

        try
        {
            return await command.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return EngineCommandResult.Fail(commandName, "Operation cancelled.");
        }
        catch (Exception ex)
        {
            return EngineCommandResult.Fail(commandName, ex.Message);
        }
    }
}
