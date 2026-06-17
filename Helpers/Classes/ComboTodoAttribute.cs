// Helpers\Classes\ComboTodoAttribute.cs
// Commands\ComboTodoAttribute.cs
namespace VS.Helper.Commands;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class ComboTodoAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public PatternType Pattern { get; set; } = PatternType.CS;
    public string SearchLabel { get; set; } = "Сканировать .slnx:";
    public string PlaceLabel { get; set; } = "Папка для результата:";
    public OperationTypes OperationTypes { get; set; } = OperationTypes.ProcessFiles;
    public bool UseBakup { get; set; }
    public bool ShowFind { get; set; }
    public bool ShowReplace { get; set; }
    public bool ShowPlace { get; set; }
    public bool ShowProject { get; set; }
    public bool ShowSampleProject { get; set; }
}
