using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Toolbelt.Blazor.I18nText.Compiler.Shared;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;

namespace Toolbelt.Blazor.I18nText.CompileTask
{
    /// <summary>
    /// Generate Localized Text Resource JSON files from Localized Text Source files.
    /// </summary>
    public class GenerateLocalizedTextResourceJson : Microsoft.Build.Utilities.Task
    {
        [Required]
        public ITaskItem[] Include { get; set; }

        public string BaseDir { get; set; }

        public string I18nTextSourceDirectory { get; set; }

        public string OutDirectory { get; set; }

        public string NameSpace { get; set; }

        public bool DisableSubNameSpace { get; set; }

        [Required]
        public string FallBackLanguage { get; set; }

        public override bool Execute()
        {
            try
            {
                if ((this.Include?.Length ?? 0) == 0)
                {
                    this.Log.LogMessage("No Source files were specified.");
                    return true;
                }

                var baseDir = this.BaseDir ?? Path.GetDirectoryName(this.BuildEngine.ProjectFileOfTaskNode);

                var options = new I18nTextCompilerOptions(baseDir);
                options.I18nTextSourceDirectory = Path.Combine(baseDir, this.I18nTextSourceDirectory) ?? options.I18nTextSourceDirectory;
                options.OutDirectory = this.OutDirectory ?? options.OutDirectory;
                options.NameSpace = string.IsNullOrEmpty(this.NameSpace) ? options.NameSpace : this.NameSpace;
                options.DisableSubNameSpace = this.DisableSubNameSpace;
                options.FallBackLanguage = this.FallBackLanguage ?? options.FallBackLanguage;
                options.LogMessage = msg => this.Log.LogMessage(msg);

                options.LogError = this.LogError;

                this.Log.LogMessage($"I18nTextSourceDirectory: [{options.I18nTextSourceDirectory}]");
                this.Log.LogMessage($"OutDirectory  : [{options.OutDirectory}]");
                this.Log.LogMessage($"FallBackLanguage: [{options.FallBackLanguage}]");

                var srcFiles = this.Include
                    .Select(item => new I18nTextSourceFile(Path.Combine(baseDir, item.ItemSpec), GetEncoding(item)))
                    .ToArray();

                foreach (var src in srcFiles) this.Log.LogMessage($"- {src.Path}, {src.Encoding.BodyName}");

                var generator = new LocalizedTextResourceJsonGenerator();
                var successOrNot = generator.Generate(srcFiles, options, CancellationToken.None);
                return successOrNot && !this.Log.HasLoggedErrors;
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e, showStackTrace: true, showDetail: true, file: null);
                return false;
            }
        }

        public void LogError(Exception exception)
        {
            if (exception is I18nTextCompileException i18nException)
            {
                this.Log.LogError(
                    subcategory: "",
                    errorCode: $"IN{(int)i18nException.Code:D3}",
                    helpKeyword: "",
                    file: i18nException.FilePath,
                    lineNumber: i18nException.LineNumber,
                    columnNumber: i18nException.LinePos,
                    endLineNumber: i18nException.LineNumber,
                    endColumnNumber: i18nException.LinePos,
                    i18nException.Message);
            }
            else
            {
                this.Log.LogError(exception.Message);
            }
        }

        private static Encoding GetEncoding(ITaskItem taskItem)
        {
            var encodingName = taskItem.GetMetadata("Encoding");
            if (string.IsNullOrEmpty(encodingName)) return Encoding.UTF8;
            return Encoding.GetEncoding(encodingName);
        }
    }
}
