using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

internal class TestConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _Options = new();

    public TestConfigOptions Add(string key, string value)
    {
        this._Options.Add(key, value);
        return this;
    }

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => this._Options.TryGetValue(key, out value);
}
