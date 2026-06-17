// Helpers\Classes\AppCleanerSolutionInfo.cs
// Commands\AppCleanerSolutionInfo.cs
namespace VS.Helper.Commands;

internal sealed class AppCleanerSolutionInfo
{
    public string SolutionPath { get; init; } = string.Empty;
    public string SolutionDir { get; init; } = string.Empty;
    public string SolutionName { get; init; } = string.Empty;
}
