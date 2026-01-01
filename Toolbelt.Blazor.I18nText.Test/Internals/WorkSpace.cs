namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal class WorkSpace : IDisposable
{
    public WorkDirectory WorkSpaceDir { get; }

    public string StartupProj { get; }

    public string Bin { get; }

    public string Obj { get; }

    public string OutputDir { get; }

    public string PublishDir { get; }

    public static string GetTestDir() => Path.Combine(FileIO.FindContainerDirToAncestor("*.slnx"), "Tests");

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

        // Prepare the project
        var wwwrootDir = Path.Combine(this.WorkSpaceDir, "Client", "wwwroot");
        var frameworkDependedIndexHtml = Path.Combine(wwwrootDir, $"index.{framework}.html");
        File.Copy(frameworkDependedIndexHtml, Path.Combine(wwwrootDir, "index.html"), overwrite: true);
        Directory.GetFiles(wwwrootDir, "index.*.html").ToList().ForEach(File.Delete);

        var componentsDir = Path.Combine(this.WorkSpaceDir, "Server", "Components");
        var frameworkDependedAppRazor = Path.Combine(componentsDir, $"App.{framework}.razor");
        File.Copy(frameworkDependedAppRazor, Path.Combine(componentsDir, "App.razor"), overwrite: true);
        Directory.GetFiles(componentsDir, "App.*.razor").ToList().ForEach(File.Delete);
    }

    public void Dispose() => this.WorkSpaceDir.Dispose();
}
