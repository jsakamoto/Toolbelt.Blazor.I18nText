using System.Collections;
using Microsoft.Build.Framework;

namespace Toolbelt.Blazor.I18nText.Test.Internals;
internal class BuildEngine : IBuildEngine
{
    private readonly List<BuildErrorEventArgs> _LoggedBuildErrors = new();

    public IEnumerable<BuildErrorEventArgs> LoggedBuildErrors => this._LoggedBuildErrors;

    public bool ContinueOnError { get; }
    public int LineNumberOfTaskNode { get; }
    public int ColumnNumberOfTaskNode { get; }
    public string ProjectFileOfTaskNode { get; } = "";
    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new NotImplementedException();
    public void LogCustomEvent(CustomBuildEventArgs e) { }
    public void LogErrorEvent(BuildErrorEventArgs e) => this._LoggedBuildErrors.Add(e);
    public void LogMessageEvent(BuildMessageEventArgs e) { }
    public void LogWarningEvent(BuildWarningEventArgs e) { }
}
