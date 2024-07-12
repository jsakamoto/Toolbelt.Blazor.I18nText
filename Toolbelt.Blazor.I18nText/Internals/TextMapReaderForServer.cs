using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals;

internal class TextMapReaderForServer : ITextMapReader
{
    private readonly IFileProvider _fileProvider;

    private readonly Uri _baseUri;

    public TextMapReaderForServer(IServiceProvider services, IOptions<StaticFileOptions> staticFileOptions)
    {
        this._fileProvider = GetFileProvider(services, staticFileOptions);

        var appDomainBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        var baseDir = Path.Combine(appDomainBaseDir, "wwwroot");
        if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar) baseDir += Path.DirectorySeparatorChar;
        this._baseUri = new Uri(baseDir);
    }

    private static IFileProvider GetFileProvider(IServiceProvider services, IOptions<StaticFileOptions> staticFileOptions)
    {
        var fileProvider = staticFileOptions.Value.FileProvider;
        if (fileProvider is not null) return fileProvider;

        var aspNetCoreHosting = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName?.StartsWith("Microsoft.AspNetCore.Hosting.Abstractions") == true);
        var webHostEnvType = aspNetCoreHosting?.ExportedTypes.FirstOrDefault(t => t.FullName == "Microsoft.AspNetCore.Hosting.IWebHostEnvironment");
        var webHostEnv = webHostEnvType is not null ? services.GetService(webHostEnvType) : null;
        return webHostEnvType?.GetProperty("WebRootFileProvider")?.GetValue(webHostEnv) as IFileProvider ?? throw new Exception("Could not retrieve the IFileProvider instance.");
    }

    public async ValueTask<Dictionary<string, string>?> ReadAsync(string jsonUrl, string textTableHash)
    {
        var fileInfo = this._fileProvider.GetFileInfo(jsonUrl);
        var jsonLocalPath = fileInfo.Exists ? fileInfo.PhysicalPath : new Uri(this._baseUri, relativeUri: jsonUrl).LocalPath;
        if (File.Exists(jsonLocalPath))
        {
            var jsonText = await File.ReadAllTextAsync(jsonLocalPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>?>(jsonText);
        }
        return default;
    }
}
