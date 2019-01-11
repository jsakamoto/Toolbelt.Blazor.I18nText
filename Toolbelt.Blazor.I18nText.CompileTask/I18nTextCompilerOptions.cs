using System;
using System.IO;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nTextCompilerOptions
    {
        public string TypesDirectory { get; set; }

        public string OutDirectory { get; set; }

        public string NameSpace { get; set; }

        public Action<string> LogMessage { get; set; }

        public I18nTextCompilerOptions() : this(Directory.GetCurrentDirectory())
        {
        }

        public I18nTextCompilerOptions(string baseDir)
        {
            this.TypesDirectory = Path.Combine(baseDir, "i18ntext", "@types");
            this.OutDirectory = Path.Combine(baseDir, "wwwroot", "content", "i18ntext");
            this.NameSpace = Path.GetFileName(baseDir.TrimEnd('\\')) + ".I18nText";
            this.LogMessage = _ => { };
        }
    }
}
