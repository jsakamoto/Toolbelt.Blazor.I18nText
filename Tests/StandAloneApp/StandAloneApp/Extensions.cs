using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace StandAloneApp
{
    public static class Extensions
    {
        public static string Format(this string format, object arg0) => format == null ? "" : string.Format(format, arg0);

        public static MarkupString AsMarkdownToHtml(this string value)
        {
            value = Regex.Replace(
                value,
                @"\[(?<TXT>[^\]]+)\]\((?<URL>[^\)]+)\)",
                m => { return "<a target=\"_blank\" href=\"" + m.Groups["URL"].Value + "\">" + m.Groups["TXT"].Value + "</a>"; });

            return new MarkupString(value);
        }
    }
}
