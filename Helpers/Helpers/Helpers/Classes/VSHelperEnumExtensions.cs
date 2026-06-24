// Helpers\Classes\EnumExtensions.cs
// Commands\EnumExtensions.cs
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace VS.Helper.Commands;

internal static class VSHelperEnumExtensions
{
    public static TAttribute GetAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
    {
        FieldInfo field = value.GetType().GetField(value.ToString());

        if (field == null)
            return null;

        return field.GetCustomAttributes(typeof(TAttribute), false)
            .Cast<TAttribute>()
            .FirstOrDefault();
    }

    public static string GetDescription(this Enum value)
    {
        DescriptionAttribute attr = value.GetAttribute<DescriptionAttribute>();
        return attr == null ? value.ToString() : attr.Description;
    }
}
