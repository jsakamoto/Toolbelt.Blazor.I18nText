using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;

namespace Toolbelt.Blazor.I18nText.Test
{
    public class WatchTest
    {
        [Test]
        public async Task Watch_Test()
        {
            using var workSpace = WorkSpace.Create("Components");

            using var restoreProcess = await Start("dotnet", "restore", workSpace.StartupProj).WaitForExitAsync();
            restoreProcess.ExitCode.Is(0, message: restoreProcess.Output);

            // 1st. Given
            var dstTextEnJsonPath = Path.Combine(workSpace.StartupProj, "bin", "Debug", "net5.0", "wwwroot", "_content", "i18ntext", "SampleSite.Components.I18nText.Text.en.json");
            File.Exists(dstTextEnJsonPath).IsFalse();

            // 1st. When
            using var watchProcess = Start("dotnet", "watch msbuild -nologo -t:CompileI18nText -v:q", workSpace.StartupProj);

            await watchProcess.WaitForOutput(output => output == "watch : Waiting for a file to change before restarting dotnet...", millsecondsTimeout: 10000);
            watchProcess.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

            // 1st. Then
            File.Exists(dstTextEnJsonPath).IsTrue();
            var dstTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dstTextEnJsonPath));
            dstTextEn["HelloWorld"].Is("Hello, world!");

            // 2nd. When
            var srcTextEnJsonPath = Path.Combine(workSpace.StartupProj, "i18ntext", "Text.en.json");
            var srcTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(srcTextEnJsonPath));
            srcTextEn["HelloWorld"] = "Hello, world.";
            File.WriteAllText(srcTextEnJsonPath, JsonSerializer.Serialize(srcTextEn));

            await watchProcess.WaitForOutput(output => output == "watch : Waiting for a file to change before restarting dotnet...", millsecondsTimeout: 10000);
            watchProcess.GetAndClearBufferedOutput().Contains("ERROR").IsFalse();

            // 2nd. Then
            dstTextEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dstTextEnJsonPath));
            dstTextEn["HelloWorld"].Is("Hello, world.");
        }
    }
}
