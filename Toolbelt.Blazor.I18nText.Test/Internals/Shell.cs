using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Toolbelt.Blazor.I18nText.Test.Internals
{
    public static class Shell
    {
        public static bool Exists(string dir, string wildCard)
        {
            return Directory.GetFiles(dir, wildCard, SearchOption.TopDirectoryOnly).Any();
        }

        public static void Delete(string dir)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }

        public static Process Run(string workDir, params string[] args)
        {
            var pi = new ProcessStartInfo
            {
                WorkingDirectory = workDir,
                FileName = args.First(),
                Arguments = string.Join(" ", args.Skip(1)),
                UseShellExecute = false,
            };
            var process = Process.Start(pi);
            process.WaitForExit();
            return process;
        }

        public static void XcopyDir(string srcDir, string dstDir, string[] excludesDir = null)
        {
            if (excludesDir == null) excludesDir = new string[0];
            var excludesDirHash = excludesDir.Select(dir => dir.ToLower()).ToHashSet();

            Directory.CreateDirectory(dstDir);

            var srcFileNames = Directory.GetFiles(srcDir);
            foreach (var srcFileName in srcFileNames)
            {
                var dstFileName = Path.Combine(dstDir, Path.GetFileName(srcFileName));
                File.Copy(srcFileName, dstFileName);
            }

            var srcSubDirs = Directory.GetDirectories(srcDir);
            foreach (var srcSubDir in srcSubDirs)
            {
                var dirName = Path.GetFileName(srcSubDir);
                if (excludesDirHash.Contains(dirName.ToLower())) continue;

                var dstSubDir = Path.Combine(dstDir, dirName);
                XcopyDir(srcSubDir, dstSubDir, excludesDir);
            }
        }
    }
}
