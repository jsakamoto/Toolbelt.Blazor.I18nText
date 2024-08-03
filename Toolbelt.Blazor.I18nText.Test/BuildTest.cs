using System.Text.Json;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test;

[Parallelizable(ParallelScope.All)]
public class BuildTest
{
    public static readonly IEnumerable<object[]> Projects =
        from startupProjName in new[] { "Client", "Host", "Server" }
        from framework in new[] { "net6.0", "net8.0" }
        select new object[] { startupProjName, framework };

    private static readonly IEnumerable<(string FileName, string Key, string Value)> ExpectedTextResJsons = [
        ("Lib4PackRef.I18nText.Text.en.json", "Message", "This Blazor component is defined in the Lib4PackRef package."),
        ("Lib4PackRef.I18nText.Text.ja.json", "Message", "この Blazor コンポーネントは Lib4PackRef で定義されています。"),
        ("Lib4PackRef6.I18nText.Text.en.json", "Message", "This Blazor component is defined in the Lib4PackRef6 package."),
        ("Lib4PackRef6.I18nText.Text.ja.json", "Message", "この Blazor コンポーネントは Lib4PackRef6 で定義されています。"),
        ("Lib4ProjRef.I18nText.Text.en.json", "Message", "This Blazor component is defined in the Lib4ProjRef package."),
        ("Lib4ProjRef.I18nText.Text.ja.json","Message", "この Blazor コンポーネントは Lib4ProjRef で定義されています。"),
        ("SampleSite.Components.I18nText.Text.en.json","HelloWorld", "Hello, world!"),
        ("SampleSite.Components.I18nText.Text.ja.json", "HelloWorld", "こんにちは、世界!")];

    [Test, TestCaseSource(nameof(Projects))]
    public async Task RunTest(string startupProjName, string framework)
    {
        // Given
        using var workSpace = new WorkSpace(startupProjName, framework, configuration: "Debug");
        var baseUrl = $"http://localhost:{Network.GetAvailableTCPPort()}";

        // When
        using var dotnetRun = Start("dotnet", $"run -f:{framework} -p:BlazorEnableCompression=false -p:CompressionEnabled=false --no-launch-profile", workSpace.StartupProj, options =>
        {
            options.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
            options.EnvironmentVariables["DOTNET_URLS"] = baseUrl;
        });
        var notTimeout = await dotnetRun.WaitForOutputAsync(
            output => output.Trim().StartsWith("Now listening on: http://localhost:"),
            options => options.IdleTimeout = 15000);
        notTimeout.IsTrue();
        dotnetRun.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

        // Then
        using var client = new HttpClient();
        foreach (var expectedTextResJson in ExpectedTextResJsons)
        {
            var res = await client.GetAsync(new Uri($"{baseUrl}/_content/i18ntext/{expectedTextResJson.FileName}"));
            res.IsSuccessStatusCode.IsTrue(message: $"{res.StatusCode} at {expectedTextResJson.FileName}");

            var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(await res.Content.ReadAsStringAsync()) ?? [];
            texts[expectedTextResJson.Key].Is(expectedTextResJson.Value);
        }
    }

    [Test, TestCaseSource(nameof(Projects))]
    public async Task PublishTest(string startupProjName, string framework)
    {
        // Given
        using var workSpace = new WorkSpace(startupProjName, framework, configuration: "Release");
        var wwwrootContentDir = Path.Combine(workSpace.PublishDir, "wwwroot", "_content");
        var i18nDistDir = Path.Combine(wwwrootContentDir, "i18ntext");
        var staticWebAssetDir = Path.Combine(wwwrootContentDir, "Toolbelt.Blazor.I18nText");

        // When
        await Start("dotnet", $"publish -c:Release -f:{framework} -p:UsingBrowserRuntimeWorkload=false -p:BlazorEnableCompression=false -p:CompressionEnabled=false --nologo", workSpace.StartupProj).ExitCodeIs(0);

        // Then

        // 1. Support client JavaScript file should be published into "_content/{PackageId}" folder.
        FileIO.ExistsAnyFilesInDir(staticWebAssetDir, "helper.min.js").IsTrue();
        FileIO.ExistsAnyFilesInDir(staticWebAssetDir, "Toolbelt.Blazor.I18nText.lib.module.js").IsTrue();

        // 2. Text resource json files have been generated under the publish folder.
        var actualTextResJsons = Directory.GetFiles(i18nDistDir, "*.json");
        foreach (var expectedTextResJson in ExpectedTextResJsons)
        {
            var expectedTextResJsonPath = Path.Combine(i18nDistDir, expectedTextResJson.FileName);
            actualTextResJsons.Contains(expectedTextResJsonPath).IsTrue(message: $"NotFound at {expectedTextResJson.FileName}");

            var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(expectedTextResJsonPath)) ?? [];
            texts[expectedTextResJson.Key].Is(expectedTextResJson.Value);
        }
    }
}
