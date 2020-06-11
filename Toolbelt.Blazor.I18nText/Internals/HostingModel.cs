using System.Runtime.InteropServices;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal static class HostingModel
    {
        public static readonly bool IsWasm = RuntimeInformation.OSDescription == "web";
    }
}
