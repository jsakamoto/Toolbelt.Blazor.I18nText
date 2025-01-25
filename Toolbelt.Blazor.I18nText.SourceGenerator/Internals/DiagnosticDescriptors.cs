using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Internals
{
    internal static class DiagnosticDescriptors
    {
        private static readonly IReadOnlyDictionary<DiagnosticCode, DiagnosticDescriptor> _Descriptors = new Dictionary<DiagnosticCode, DiagnosticDescriptor>
        {
            [DiagnosticCode.Information] = new DiagnosticDescriptor(
                id: "I18N000",
                title: "Information",
                messageFormat: "{0}",
                category: "I18nText",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true),

            [DiagnosticCode.FallbackLangNotFound] = new DiagnosticDescriptor(
                id: "I18N001",
                title: "Couldn't find a localized text source file for fallback language",
                messageFormat: "{0}",
                category: "I18nText",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),

            [DiagnosticCode.SourceTextIsInvalidFormat] = new DiagnosticDescriptor(
                id: "I18N002",
                title: "A localized text source file is invalid format",
                messageFormat: "{0}",
                category: "I18nText",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),

            [DiagnosticCode.UnknownFileType] = new DiagnosticDescriptor(
                id: "I18N003",
                title: "Unknown file type as an I18n text source",
                messageFormat: "{0}",
                category: "I18nText",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),

            [DiagnosticCode.UnhandledException] = new DiagnosticDescriptor(
                id: "I18N999",
                title: "Unhandled exception",
                messageFormat: "{0}",
                category: "I18nText",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
        };

        public static DiagnosticDescriptor Information => _Descriptors[DiagnosticCode.Information];

        public static DiagnosticDescriptor UnhandledException => _Descriptors[DiagnosticCode.UnhandledException];

        public static bool TryGetByCode(DiagnosticCode code, out DiagnosticDescriptor descriptor) => _Descriptors.TryGetValue(code, out descriptor);
    }
}
