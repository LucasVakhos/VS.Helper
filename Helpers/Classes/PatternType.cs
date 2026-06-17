// Helpers\Classes\PatternType.cs
// Commands\PatternType.cs
using System.ComponentModel;

namespace VS.Helper.Commands;

internal enum PatternType
{
    [Description("*.cs")]
    CS,

    [Description("*.txt")]
    TXT,

    [Description("*.razor")]
    RAZOR,

    [Description("*.bak")]
    BAK,

    [Description("*.*")]
    ALL
}
