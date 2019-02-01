using System.Reflection;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText
{
    public static class I18nTextExtensions
    {
        public static string GetFieldValue<T>(this T value, string key) where T : I18nTextLateBinding
        {
            return typeof(T).GetField(key, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) as string ?? key;
        }
    }
}
