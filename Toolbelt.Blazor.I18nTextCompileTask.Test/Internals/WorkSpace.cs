namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal class WorkSpace : IDisposable
{
    public WorkDirectory WorkSpaceDir { get; }

    public string ProjectDir { get; }

    public string I18nTextDir { get; }

    public string TypesDir { get; }

    public string TextResJsonsDir { get; }

    public WorkSpace(string i18ntextSubDirName = "i18ntext")
    {
        const string projectName = "Toolbelt.Blazor.I18nTextCompileTask.Test";

        var testProjectDir = FileIO.FindContainerDirToAncestor("*.csproj");
        var srcI18nTextDir = Path.Combine(testProjectDir, i18ntextSubDirName);

        this.WorkSpaceDir = new WorkDirectory();
        FileIO.XcopyDir(srcI18nTextDir, Path.Combine(this.WorkSpaceDir, projectName, i18ntextSubDirName));

        this.ProjectDir = Path.Combine(this.WorkSpaceDir, projectName);
        this.I18nTextDir = Path.Combine(this.ProjectDir, i18ntextSubDirName);
        this.TypesDir = Path.Combine(this.I18nTextDir, "@types");
        this.TextResJsonsDir = Path.Combine(this.WorkSpaceDir, projectName, "obj", "Debug", "netstandard2.0", "dist", "_content", "i18ntext");
    }

    void IDisposable.Dispose() => this.WorkSpaceDir.Dispose();
}
