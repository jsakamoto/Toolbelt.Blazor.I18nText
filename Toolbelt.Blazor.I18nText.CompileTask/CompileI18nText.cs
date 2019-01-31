using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Toolbelt.Blazor.I18nText
{
    public class CompileI18nText : Microsoft.Build.Utilities.Task
    {
        [Required]
        public ITaskItem[] Include { get; set; }

        public string BaseDir { get; set; }

        public string TypesDirectory { get; set; }

        public string OutDirectory { get; set; }

        public string NameSpace { get; set; }

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
                options.TypesDirectory = this.TypesDirectory ?? options.TypesDirectory;
                options.OutDirectory = this.OutDirectory ?? options.OutDirectory;
                options.NameSpace = string.IsNullOrEmpty(this.NameSpace) ? options.NameSpace : this.NameSpace;
                options.LogMessage = msg => Log.LogMessage(msg);
                options.LogError = msg => Log.LogError(msg);
                options.FallBackLanguage = this.FallBackLanguage ?? options.FallBackLanguage;

                Log.LogMessage($"TypesDirectory: [{options.TypesDirectory}]");
                Log.LogMessage($"OutDirectory  : [{options.OutDirectory}]");
                Log.LogMessage($"NameSpace     : [{options.NameSpace}]");
                Log.LogMessage($"FallBackLanguage: [{options.FallBackLanguage}]");

                var srcFilesPath = this.Include
                    .Select(taskItem => Path.Combine(baseDir, taskItem.ItemSpec))
                    .ToArray();

                foreach (var src in srcFilesPath) Log.LogMessage($"- {src}");

                var compiler = new I18nTextCompiler();
                var successOrNot = compiler.Compile(srcFilesPath, options);
                return successOrNot;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, showStackTrace: true, showDetail: true, file: null);
                return false;
            }
        }
    }
}
