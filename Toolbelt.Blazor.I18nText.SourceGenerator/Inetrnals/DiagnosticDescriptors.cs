using Microsoft.CodeAnalysis;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Inetrnals
{
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor Information = new DiagnosticDescriptor(
            id: "I18N000",
            title: "Information",
            messageFormat: "{0}",
            category: "I18nText",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FallbackLangNotFound = new DiagnosticDescriptor(
            id: "I18N001",
            title: "Couldn't find a localized text source file for fallback language",
            messageFormat: "{0}",
            category: "I18nText",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnhandledException = new DiagnosticDescriptor(
            id: "I18N999",
            title: "Unhandled exception",
            messageFormat: "{0}",
            category: "I18nText",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        public static bool TryGetByCode(int code, out DiagnosticDescriptor descriptor)
        {
            descriptor = null;
            switch (code)
            {
                case 1: descriptor = FallbackLangNotFound; break;
                default: break;
            }
            return descriptor != null;
        }

    }
}
