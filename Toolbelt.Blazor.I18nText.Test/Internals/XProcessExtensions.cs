using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Toolbelt.Diagnostics;

namespace Toolbelt.Blazor.I18nText.Test.Internals
{
    public static class XProcessExtensions
    {
        public static async Task WaitForOutput(this XProcess process, Func<string, bool> predicate, int millsecondsTimeout)
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
}
