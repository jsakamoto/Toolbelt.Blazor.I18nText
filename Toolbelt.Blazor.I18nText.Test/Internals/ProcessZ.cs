#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Diagnostics;

namespace Toolbelt.Blazor.I18nText.Test.Internals
{
    internal class ProcessZ : IAsyncDisposable
    {
        private Task ConsumeStdoutTask { get; }

        private Task ConsumeStderrTask { get; }

        private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        private TaskCompletionSource TaskCompletionSource { get; set; } = new TaskCompletionSource();

        private List<string> StdoutBuffer { get; } = new List<string>();

        private int LastCheckedBufferIndex { get; set; } = 0;

        private Func<string, bool>? WaitForOutputPredicate { get; set; }

        private Process Process { get; }

        public int? ExitCode
        {
            get
            {
                if (this.Process == null) return null;
                var exitCodeFiled = typeof(Process).GetField("_exitCode", BindingFlags.NonPublic | BindingFlags.Instance);
                if (exitCodeFiled == null) throw new Exception("Could not retrieve the '_exitCode' field information of the 'Propcess' type.");
                return (int)exitCodeFiled.GetValue(this.Process)!;
            }
        }

        public string Output { get { lock (this.StdoutBuffer) return string.Join("\n", this.StdoutBuffer); } }

        public static ProcessZ Start(string filename, string arguments, string workingDirectory)
        {
            return new ProcessZ(filename, arguments, workingDirectory);
        }

        static ProcessZ()
        {
            ProcessX.AcceptableExitCodes = Enumerable.Range(0, 1000).ToArray();
        }

        public ProcessZ(string filename, string arguments, string workingDirectory)
        {
            var (process, stdOut, stdErr) = ProcessX.GetDualAsyncEnumerable(filename, arguments, workingDirectory);
            this.Process = process;
            var stdoutAsyncStream = stdOut.WithCancellation(this.CancellationTokenSource.Token);
            var stderrAsyncStream = stdErr.WithCancellation(this.CancellationTokenSource.Token);

            this.ConsumeStdoutTask = Task.Run(async () =>
            {
                await foreach (var stdoutText in stdoutAsyncStream)
                {
                    lock (this.StdoutBuffer) this.StdoutBuffer.Add(stdoutText);
                    if (this.WaitForOutputPredicate?.Invoke(stdoutText) == true) this.TaskCompletionSource.TrySetResult();
                }
            });

            this.ConsumeStderrTask = Task.Run(async () =>
            {
                await foreach (var stderrText in stderrAsyncStream)
                {
                    lock (this.StdoutBuffer) this.StdoutBuffer.Add(stderrText);
                    if (this.WaitForOutputPredicate?.Invoke(stderrText) == true) this.TaskCompletionSource.TrySetResult();
                }
            });
        }

        public async ValueTask WaitForOutput(Func<string, bool> predicate, int millsecondsTimeout)
        {
            lock (this.StdoutBuffer)
            {
                this.TaskCompletionSource = new TaskCompletionSource();
                this.WaitForOutputPredicate = predicate;
                for (var i = this.LastCheckedBufferIndex; i < this.StdoutBuffer.Count; i++)
                {
                    this.LastCheckedBufferIndex = i;
                    var stdoutText = this.StdoutBuffer[i];
                    if (predicate?.Invoke(stdoutText) == true)
                    {
                        this.TaskCompletionSource.TrySetResult();
                    }
                }
                this.LastCheckedBufferIndex++;
            }

            await Task.WhenAny(this.TaskCompletionSource.Task, Task.Delay(millsecondsTimeout));
            if (!this.TaskCompletionSource.Task.IsCompletedSuccessfully)
            {
                throw new TimeoutException("The process didn't respond the expected output.\n");
            }
        }

        public string[] ReadCurrentOutput()
        {
            var current = default(string[]);
            lock (this.StdoutBuffer)
            {
                current = this.StdoutBuffer.ToArray();
                this.StdoutBuffer.Clear();
                this.LastCheckedBufferIndex = 0;
            }
            return current;
        }

        public async ValueTask DisposeAsync()
        {
            if (!this.CancellationTokenSource.IsCancellationRequested) this.CancellationTokenSource.Cancel();
            try { await Task.WhenAll(ConsumeStdoutTask, ConsumeStderrTask); } catch (OperationCanceledException) { }
        }

        internal async ValueTask<ProcessZ> WaitForExitAsync()
        {
            try { await Task.WhenAll(ConsumeStdoutTask, ConsumeStderrTask); } catch (OperationCanceledException) { }
            return this;
        }
    }
}
