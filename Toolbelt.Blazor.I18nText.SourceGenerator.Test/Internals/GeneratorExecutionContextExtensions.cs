using System.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using Toolbelt.DynamicBinderExtension;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

internal static class GeneratorExecutionContextExtensions
{
    public static IEnumerable<GeneratedSourceText> GetGeneratedSourceTexts(this GeneratorExecutionContext context)
    {
        var additionalSources = context.ToDynamic()._additionalSources._sourcesAdded as DynamicBinder;
        var sourcesAdded = additionalSources!.Object.IsInstanceOf<IEnumerable>().Cast<object>();

        foreach (var source in sourcesAdded)
        {
            var hintName = (string)source.ToDynamic().HintName;
            var sourceText = (SourceText)source.ToDynamic().Text;
            yield return new GeneratedSourceText(hintName, sourceText);
        }
    }
}
