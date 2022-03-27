using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

internal class AdditionalTextImplement : AdditionalText
{
    public override string Path { get; }

    public override SourceText? GetText(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public AdditionalTextImplement(string path)
    {
        this.Path = path;
    }
}
