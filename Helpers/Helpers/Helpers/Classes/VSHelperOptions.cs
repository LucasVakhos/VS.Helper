// Helpers\Classes\VSHelperOptions.cs
// Commands\VSHelperOptions.cs
namespace VS.Helper.Commands;

internal sealed class VSHelperOptions
{
    public VSHelperComboTodoItems Item { get; set; }
    public string SolutionPath { get; set; }
    public string SolutionDir { get; set; }
    public string SearchPath { get; set; }
    public string PlacePath { get; set; }
    public string FindText { get; set; }
    public string ReplaceText { get; set; }
    public string Pattern { get; set; }
    public bool UseBackup { get; set; }
    public bool DryRun { get; set; }
}
