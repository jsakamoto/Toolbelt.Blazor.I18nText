using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Compiler.Shared;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test;

public class I18nTextCompilerOptionsTest
{
    [Test]
    public void GetFallbackLangCandidates_MultiLangs_Test()
    {
        // Given
        var options = new I18nTextCompilerOptions(baseDir: "")
        {
            FallBackLanguage = "en-US"
        };

        // When
        var candidates = options.GetFallbackLangCandidates();

        // Then
        candidates.Is("en-US", "en");
    }

    [Test]
    public void GetFallbackLangCandidates_SingleLang_Test()
    {
        // Given
        var options = new I18nTextCompilerOptions(baseDir: "")
        {
            FallBackLanguage = "ja"
        };

        // When
        var candidates = options.GetFallbackLangCandidates();

        // Then
        candidates.Is("ja");
    }
}
