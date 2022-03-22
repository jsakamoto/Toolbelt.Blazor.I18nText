namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal class WorkSpace : IDisposable
{
    public WorkDirectory WorkSpaceDir { get; }

    public string StartupProj { get; }

    public string Bin { get; }

    public string Obj { get; }

    public string OutputDir { get; }

    public string PublishDir { get; }

    public static string GetTestDir() => Path.Combine(FileIO.FindContainerDirToAncestor("*.sln"), "Tests");

    public WorkSpace(string startupProjDir, string framework, string configuration)
    {
        this.WorkSpaceDir = WorkDirectory.CreateCopyFrom(GetTestDir(), predicate: item => item.Name is not "obj" and not "bin" and not ".vs");

        var nugetConfigPath = Path.Combine(this.WorkSpaceDir, "nuget.config");
        if (File.Exists(nugetConfigPath)) File.Delete(nugetConfigPath);

        this.StartupProj = Path.Combine(this.WorkSpaceDir, startupProjDir);
        this.Bin = Path.Combine(this.StartupProj, "bin");
        this.Obj = Path.Combine(this.StartupProj, "obj");
        this.OutputDir = Path.Combine(this.Bin, configuration, framework);
        this.PublishDir = Path.Combine(this.OutputDir, "publish");
    }

    public void Dispose() => this.WorkSpaceDir.Dispose();
}
