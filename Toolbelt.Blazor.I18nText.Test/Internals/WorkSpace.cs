#nullable enable
using System;
using System.IO;
using static Toolbelt.Blazor.I18nText.Test.Internals.Shell;

namespace Toolbelt.Blazor.I18nText.Test.Internals
{
    public class WorkSpace : IDisposable
    {
        public string WorkSpaceDir { get; }

        public string StartupProj { get; }

        public string Bin { get; }

        public string Obj { get; }

        public static WorkSpace Create(string startupProjDir)
        {
            return new WorkSpace(startupProjDir);
        }

        public static string GetTestDir()
        {
            var testDir = AppDomain.CurrentDomain.BaseDirectory;
            do { testDir = Path.GetDirectoryName(testDir); } while (!Exists(testDir, "*.sln"));
            testDir = Path.Combine(testDir!, "Tests");
            return testDir;
        }

        private WorkSpace(string startupProjDir)
        {
            var testDir = GetTestDir();
            WorkSpaceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N"));
            XcopyDir(testDir, WorkSpaceDir, excludesDir: new[] { ".vs", "bin", "obj" });

            StartupProj = Path.Combine(WorkSpaceDir, startupProjDir);
            Bin = Path.Combine(StartupProj, "bin");
            Obj = Path.Combine(StartupProj, "obj");
        }

        public void Dispose()
        {
            Delete(WorkSpaceDir);
        }
    }
}
