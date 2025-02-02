using System.Text.Json;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test;

public class WatchTest
{
    public static readonly IEnumerable<object[]> Projects =
        from startupProjName in new[] { "Client", "Host", "Server" }
        from framework in new[] { "net8.0", "net9.0" }
        select new object[] { startupProjName, framework };

    [Ignore("\"dotnet watch\" temporary doesn't work on Blazor I18n Text v.14.0.")]
    [Test, TestCaseSource(nameof(Projects))]
    public async Task Watch_Test(string startupProjName, string framework)
    {
        using var workSpace = new WorkSpace(startupProjName, framework, configuration: "Debug");
        var componentsProjDir = Path.Combine(workSpace.WorkSpaceDir, "Components");

        await Start("dotnet", "restore", workSpace.StartupProj).ExitCodeIs(0);

        // 1st. Given
        var dstTextEnJsonPath = Path.Combine(componentsProjDir, "obj", "dist", "_content", "i18ntext", "SampleSite.Components.I18nText.Text.en.json");
        File.Exists(dstTextEnJsonPath).IsFalse();

        // 1st. When: Start the "dotnet watch" process.
        var arguments = new[]{
            "watch",
            "-f:" + framework,
            "--nologo",
            "--no-restore",
            "--non-interactive",
            "--no-launch-profile",
            "--property:BlazorEnableCompression=false",
            "--property:CompressionEnabled=false"
        };
        using var watchProcess = Start("dotnet", string.Join(' ', arguments), workSpace.StartupProj, options =>
        {
            options.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
            options.EnvironmentVariables["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "true";
            options.EnvironmentVariables["DOTNET_URLS"] = $"http://localhost:{Network.GetAvailableTCPPort()}";
        });

        var notTimeout = await watchProcess.WaitForOutputAsync(
            output => output.Trim().StartsWith("Now listening on: http://localhost:"),
            options => options.IdleTimeout = 15000);
        notTimeout.IsTrue(message: watchProcess.Output);
        watchProcess.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

        // 1st. Then: The text resource json files from projects have been generated under the output folder.
        File.Exists(dstTextEnJsonPath).IsTrue();
        var dstTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dstTextEnJsonPath)) ?? [];
        dstTextEn["HelloWorld"].Is("Hello, world!");

        // 2nd. When
        var srcTextEnJsonPath = Path.Combine(componentsProjDir, "i18ntext", "Text.en.json");
        var srcTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(srcTextEnJsonPath)) ?? [];
        srcTextEn["HelloWorld"] = "Hello, world.";
        File.WriteAllText(srcTextEnJsonPath, JsonSerializer.Serialize(srcTextEn));

        notTimeout = await watchProcess.WaitForOutputAsync(
            output => output.Trim().StartsWith("Now listening on: http://localhost:"),
            options => options.IdleTimeout = 5000);
        notTimeout.IsTrue();
        watchProcess.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

        // 2nd. Then
        dstTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dstTextEnJsonPath)) ?? [];
        dstTextEn["HelloWorld"].Is("Hello, world.");
    }
}
