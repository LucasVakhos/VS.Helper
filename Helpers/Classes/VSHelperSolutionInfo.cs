// Helpers\Classes\VSHelperSolutionInfo.cs
// Commands\VSHelperSolutionInfo.cs
namespace VS.Helper.Commands;

internal sealed class VSHelperSolutionInfo
{
    public string SolutionPath { get; init; } = string.Empty;
    public string SolutionDir { get; init; } = string.Empty;
    public string SolutionName { get; init; } = string.Empty;
}
