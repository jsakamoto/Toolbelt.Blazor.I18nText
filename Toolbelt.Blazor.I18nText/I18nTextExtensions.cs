using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText;

public static class I18nTextExtensions
{
    private static class FieldsCache<T>
    {
        public static readonly IReadOnlyDictionary<string, FieldInfo> Fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(f => f.Name, f => f);
    }

    public static string? GetFieldValue<T>(this T value, string? key) where T : I18nTextLateBinding
    {
        return value.GetFieldValueWithNoFallback(key) ?? key;
    }

    public static string? GetFieldValueWithNoFallback<T>(this T value, string? key) where T : I18nTextLateBinding
    {
        if (string.IsNullOrEmpty(key)) return "";
        return FieldsCache<T>.Fields.TryGetValue(key!, out var field) ? field.GetValue(value) as string : null;
    }
}
