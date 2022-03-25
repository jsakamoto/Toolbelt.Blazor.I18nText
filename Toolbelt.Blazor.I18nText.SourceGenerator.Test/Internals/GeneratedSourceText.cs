using Microsoft.CodeAnalysis.Text;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

internal readonly struct GeneratedSourceText
{
    public SourceText Text { get; }

    public string HintName { get; }

    public GeneratedSourceText(string hintName, SourceText text)
    {
        this.Text = text;
        this.HintName = hintName;
    }
}