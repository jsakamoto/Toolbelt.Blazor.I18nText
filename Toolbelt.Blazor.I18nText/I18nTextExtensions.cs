using System.Reflection;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText
{
    public static class I18nTextExtensions
    {
        public static string? GetFieldValue<T>(this T value, string? key) where T : I18nTextLateBinding
        {
            return value.GetFieldValueWithNoFallback(key) ?? key;
        }

        public static string? GetFieldValueWithNoFallback<T>(this T value, string? key) where T : I18nTextLateBinding
        {
            return value.GetType().GetField(key, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) as string;
        }
    }
}
