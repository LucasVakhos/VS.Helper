using System.Threading;
using System.Threading.Tasks;

namespace VS.Helper.Core.OS;

public interface IEngineCommand
{
    string Name { get; }
    Task<EngineCommandResult> ExecuteAsync(EngineContext context, CancellationToken cancellationToken);
}
