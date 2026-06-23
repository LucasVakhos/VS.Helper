// Helpers\Classes\ComboTodoAttribute.cs
// Commands\ComboTodoAttribute.cs
using System;
namespace VS.Helper.Commands;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class VSHelperComboTodoAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public VSHelperPatternType Pattern { get; set; } = VSHelperPatternType.CS;
    public string SearchLabel { get; set; } = "Сканировать .slnx:";
    public string PlaceLabel { get; set; } = "Папка для результата:";
    public VSHelperOperationTypes OperationTypes { get; set; } = VSHelperOperationTypes.ProcessFiles;
    public bool UseBakup { get; set; }
    public bool ShowFind { get; set; }
    public bool ShowReplace { get; set; }
    public bool ShowPlace { get; set; }
    public bool ShowProject { get; set; }
    public bool ShowSampleProject { get; set; }
}
