using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText;

public static class I18nTextExtensions
{
    private static class FieldsCache<T>
    {
#pragma warning disable IL2090
        public static readonly IReadOnlyDictionary<string, FieldInfo> Fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(f => f.Name, f => f);
#pragma warning restore IL2090
    }

    public static string? GetFieldValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(this T value, string? key) where T : I18nTextLateBinding
    {
        return value.GetFieldValueWithNoFallback(key) ?? key;
    }

    public static string? GetFieldValueWithNoFallback<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(this T value, string? key) where T : I18nTextLateBinding
    {
        if (string.IsNullOrEmpty(key)) return "";
        return FieldsCache<T>.Fields.TryGetValue(key!, out var field) ? field.GetValue(value) as string : null;
    }
}
