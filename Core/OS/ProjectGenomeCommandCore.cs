using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VS.Helper.Core.OS;

public sealed class ProjectGenomeCommandCore : IEngineCommand
{
    public const string CommandName = "project-genome";
    public string Name => CommandName;

    public Task<EngineCommandResult> ExecuteAsync(EngineContext context, CancellationToken cancellationToken)
    {
        ProjectGenome genome = new ProjectGenomeAnalyzer().Analyze(context.SolutionPath, cancellationToken);
        string outputPath = Path.Combine(context.WorkDirectory, "project-genome.json");
        string json = JsonSerializer.Serialize(genome, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);

        EngineCommandResult result = EngineCommandResult.Ok(Name, "Project Genome created: " + outputPath);
        result.Data["OutputPath"] = outputPath;
        result.Data["Projects"] = genome.ProjectCount.ToString();
        result.Data["Files"] = genome.SourceFileCount.ToString();
        result.Data["Lines"] = genome.ApproxLinesOfCode.ToString();
        result.Data["Todos"] = genome.TodoCount.ToString();
        return Task.FromResult(result);
    }
}
