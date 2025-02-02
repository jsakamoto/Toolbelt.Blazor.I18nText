namespace Toolbelt.Blazor.I18nText.CompileTask.Test.Internals;

internal class WorkSpace : IDisposable
{
    public WorkDirectory WorkSpaceDir { get; }

    public string ProjectDir { get; }

    /// <summary>
    /// Gets the directory path of the Localized Text Source files.<br/>
    /// By default, it is "~/i18ntext".
    /// </summary>
    public string I18nTextDir { get; }

    /// <summary>
    /// Get the the output directory path of the generated Localized Text Resource JSON files.<br/>
    /// it is "~/obj/Debug/net8.0/dist/_content/i18ntext".
    /// </summary>
    public string TextResJsonsDir { get; }

    public WorkSpace(string i18ntextSubDirName = "i18ntext")
    {
        const string projectName = "Toolbelt.Blazor.I18nText.CompileTask.Test";

        var testProjectDir = FileIO.FindContainerDirToAncestor("*.csproj");
        var srcI18nTextDir = Path.Combine(testProjectDir, i18ntextSubDirName);

        this.WorkSpaceDir = new WorkDirectory();
        FileIO.XcopyDir(srcI18nTextDir, Path.Combine(this.WorkSpaceDir, projectName, i18ntextSubDirName));

        this.ProjectDir = Path.Combine(this.WorkSpaceDir, projectName);
        this.I18nTextDir = Path.Combine(this.ProjectDir, i18ntextSubDirName);
        this.TextResJsonsDir = Path.Combine(this.ProjectDir, Path.Combine("obj/Debug/net8.0/dist/_content/i18ntext".Split('/')));
    }

    void IDisposable.Dispose() => this.WorkSpaceDir.Dispose();
}
