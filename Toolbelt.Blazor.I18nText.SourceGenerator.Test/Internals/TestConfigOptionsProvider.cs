using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

internal class TestConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly TestConfigOptions _GlobalOptions = new();

    public override AnalyzerConfigOptions GlobalOptions => this._GlobalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        throw new NotImplementedException();
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        throw new NotImplementedException();
    }

    public TestConfigOptionsProvider ConfigureGlobalOptions(Action<TestConfigOptions> configure)
    {
        configure(this._GlobalOptions);
        return this;
    }
}
