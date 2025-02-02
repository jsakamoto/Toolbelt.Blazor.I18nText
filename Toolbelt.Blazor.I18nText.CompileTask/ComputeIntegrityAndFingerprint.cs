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
                var hash = ComputeHash(sha256, item);
                item.SetMetadata("Integrity", Convert.ToBase64String(hash));
                item.SetMetadata("Fingerprint", Base36.Encode(hash));
            }

            this.ComputedItems = this.Items;
            return true;
        }

        private static byte[] ComputeHash(SHA256 sha256, ITaskItem item)
        {
            var path = item.GetMetadata("OriginalItemSpec");
            if (!File.Exists(path)) path = item.GetMetadata("FullPath");
            if (!File.Exists(path)) path = item.GetMetadata("Identity");
            var contentBytes = File.ReadAllBytes(path);
            return sha256.ComputeHash(contentBytes);
        }
    }
}
