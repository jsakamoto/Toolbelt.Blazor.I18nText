using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;

namespace Toolbelt.Blazor.I18nText.CompileTask
{
    /// <summary>
    /// Compute Integrity and Fingerprint for each item.
    /// </summary>
    public class ComputeIntegrityAndFingerprint : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Items to compute Integrity and Fingerprint.
        /// </summary>
        [Required]
        public ITaskItem[] Items { get; set; }

        /// <summary>
        /// Computed items with Integrity and Fingerprint.
        /// </summary>
        [Output]
        public ITaskItem[] ComputedItems { get; set; }

        public override bool Execute()
        {
            using var sha256 = SHA256.Create();
            foreach (var item in this.Items)
            {
                var path = GetFilePath(item);
                var fileInfo = new FileInfo(path);
                var hash = ComputeHash(sha256, path);
                item.SetMetadata("Integrity", Convert.ToBase64String(hash));
                item.SetMetadata("Fingerprint", Base36.Encode(hash));
                item.SetMetadata("FileLength", fileInfo.Length.ToString());
                item.SetMetadata("LastWriteTime", fileInfo.LastWriteTimeUtc.ToString("R")); // ex."Tue, 20 May 2025 23:21:08 GMT"
            }

            this.ComputedItems = this.Items;
            return true;
        }

        private static byte[] ComputeHash(SHA256 sha256, string path)
        {
            var contentBytes = File.ReadAllBytes(path);
            return sha256.ComputeHash(contentBytes);
        }

        private static string GetFilePath(ITaskItem item)
        {
            var path = item.GetMetadata("OriginalItemSpec");
            if (!File.Exists(path)) path = item.GetMetadata("FullPath");
            if (!File.Exists(path)) path = item.GetMetadata("Identity");
            return path;
        }
    }
}
