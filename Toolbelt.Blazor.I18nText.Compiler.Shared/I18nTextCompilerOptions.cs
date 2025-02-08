using System;
using System.IO;

namespace Toolbelt.Blazor.I18nText.Compiler.Shared
{
    public class I18nTextCompilerOptions
    {
        public string I18nTextSourceDirectory { get; set; }

        public string OutDirectory { get; set; }

        public string NameSpace { get; set; }

        public bool DisableSubNameSpace { get; set; }

        public Action<string> LogMessage { get; set; }

        public Action<Exception> LogError { get; set; }

        public string FallBackLanguage { get; set; }

        public I18nTextCompilerOptions() : this(Directory.GetCurrentDirectory())
        {
        }

        public I18nTextCompilerOptions(string baseDir)
        {
            this.I18nTextSourceDirectory = Path.Combine(baseDir, "i18ntext");
            this.OutDirectory = Path.Combine(baseDir, "obj", "dist", "_content", "i18ntext");
            this.NameSpace = Path.GetFileName(baseDir.TrimEnd('\\')) + ".I18nText";
            this.LogMessage = _ => { };
            this.LogError = _ => { };
            this.FallBackLanguage = "en";
        }

        /// <summary>
        /// Get the fallback language candidates, like ["en-US", "en"] or ["ja"]
        /// </summary>
        public string[] GetFallbackLangCandidates()
        {
            var fallbackLangParts = this.FallBackLanguage.Split('-');
            return fallbackLangParts.Length > 1 ? new[] { this.FallBackLanguage, fallbackLangParts[0] } : new[] { this.FallBackLanguage };
        }
    }
}
