using NUnit.Framework;
using Toolbelt.Diagnostics;

namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal static class XProcessExtensions
{
    public static async ValueTask ExitCodeIs(this XProcess process, int expected)
    {
        using (process)
        {
            await process.WaitForExitAsync();
            process.ExitCode.Is(expected, "message: " + process.Output);
        }
    }

    public static async ValueTask WaitForOutput(this XProcess process, Func<string, bool> predicate, int millsecondsTimeout)
    {
        var bufferedOutput = process.GetAndClearBufferedOutput();
        if (predicate(bufferedOutput)) return;

        var cts = new CancellationTokenSource(millsecondsTimeout);
        await foreach (var output in process.GetOutputAsyncStream().WithCancellation(cts.Token))
        {
            if (predicate(output)) return;
        }
    }
}
