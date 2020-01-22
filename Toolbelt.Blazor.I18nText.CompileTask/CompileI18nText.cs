using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace Toolbelt.Blazor.I18nText
{
    public class CompileI18nText : Microsoft.Build.Utilities.Task
    {
        [Required]
        public ITaskItem[] Include { get; set; }

        public string BaseDir { get; set; }

        public string I18nTextSourceDirectory { get; set; }

        public string TypesDirectory { get; set; }

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
                    Log.LogMessage("No Source files were specified.");
                    return true;
                }

                var baseDir = this.BaseDir ?? Path.GetDirectoryName(this.BuildEngine.ProjectFileOfTaskNode);

                var options = new I18nTextCompilerOptions(baseDir);
                options.I18nTextSourceDirectory = Path.Combine(baseDir, this.I18nTextSourceDirectory) ?? options.I18nTextSourceDirectory;
                options.TypesDirectory = this.TypesDirectory ?? options.TypesDirectory;
                options.OutDirectory = this.OutDirectory ?? options.OutDirectory;
                options.NameSpace = string.IsNullOrEmpty(this.NameSpace) ? options.NameSpace : this.NameSpace;
                options.DisableSubNameSpace = this.DisableSubNameSpace;
                options.LogMessage = msg => Log.LogMessage(msg);
                options.LogError = msg => Log.LogError(msg);
                options.FallBackLanguage = this.FallBackLanguage ?? options.FallBackLanguage;

                Log.LogMessage($"I18nTextSourceDirectory: [{options.I18nTextSourceDirectory}]");
                Log.LogMessage($"TypesDirectory: [{options.TypesDirectory}]");
                Log.LogMessage($"OutDirectory  : [{options.OutDirectory}]");
                Log.LogMessage($"NameSpace     : [{options.NameSpace}]");
                Log.LogMessage($"FallBackLanguage: [{options.FallBackLanguage}]");

                var srcFiles = this.Include
                    .Select(taskItem => (Path: Path.Combine(baseDir, taskItem.ItemSpec), Encoding: GetEncoding(taskItem)))
                    .Select(item => new I18nTextSourceFile(item.Path, item.Encoding))
                    .ToArray();

                foreach (var src in srcFiles) Log.LogMessage($"- {src.Path}, {src.Encoding.BodyName}");

                var compiler = new I18nTextCompiler();
                var successOrNot = compiler.Compile(srcFiles, options);
                return successOrNot;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, showStackTrace: true, showDetail: true, file: null);
                return false;
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
