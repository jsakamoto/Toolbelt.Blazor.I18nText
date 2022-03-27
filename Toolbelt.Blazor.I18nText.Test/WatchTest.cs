using System.Text.Json;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test;

[Parallelizable(ParallelScope.All)]
public class WatchTest
{
    public static readonly IEnumerable<object[]> Frameworks =
        from framwork in new[] { "net6.0", /*"net7.0" */}
        select new object[] { framwork };

    [Test, TestCaseSource(nameof(Frameworks))]
    public async Task Watch_Test(string framework)
    {
        using var workSpace = new WorkSpace("Components", framework, configuration: "Debug");

        await Start("dotnet", "restore", workSpace.StartupProj).ExitCodeIs(0);

        // 1st. Given
        var dstTextEnJsonPath = Path.Combine(workSpace.OutputDir, "wwwroot", "_content", "i18ntext", "SampleSite.Components.I18nText.Text.en.json");
        File.Exists(dstTextEnJsonPath).IsFalse();

        // 1st. When
        using var watchProcess = Start("dotnet", $"watch build -f:{framework} --nologo", workSpace.StartupProj);

        await watchProcess.WaitForOutput(output => output == "watch : Waiting for a file to change before restarting dotnet...", millsecondsTimeout: 10000);
        watchProcess.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

        // 1st. Then
        File.Exists(dstTextEnJsonPath).IsTrue();
        var dstTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dstTextEnJsonPath)) ?? new();
        dstTextEn["HelloWorld"].Is("Hello, world!");

        // 2nd. When
        var srcTextEnJsonPath = Path.Combine(workSpace.StartupProj, "i18ntext", "Text.en.json");
        var srcTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(srcTextEnJsonPath)) ?? new();
        srcTextEn["HelloWorld"] = "Hello, world.";
        File.WriteAllText(srcTextEnJsonPath, JsonSerializer.Serialize(srcTextEn));

        await watchProcess.WaitForOutput(output => output == "watch : Waiting for a file to change before restarting dotnet...", millsecondsTimeout: 10000);
        watchProcess.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

        // 2nd. Then
        dstTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dstTextEnJsonPath)) ?? new();
        dstTextEn["HelloWorld"].Is("Hello, world.");
    }
}
