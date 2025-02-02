using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals;

[SuppressMessage("Trimming", "IL2075"), SuppressMessage("Trimming", "IL2026")]
internal class TextMapReaderForServer : ITextMapReader
{
    private readonly Uri _baseUri;

    private readonly Func<string, string?> _getPhysicalPath;

    public TextMapReaderForServer(IServiceProvider services)
    {
        this._getPhysicalPath = CreatePhysicalPathGetter(services);

        var appDomainBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        var baseDir = Path.Combine(appDomainBaseDir, "wwwroot");
        if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar) baseDir += Path.DirectorySeparatorChar;
        this._baseUri = new Uri(baseDir);
    }

    private static Func<string, string?> CreatePhysicalPathGetter(IServiceProvider services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Assembly? getAssembly(string name) => assemblies.FirstOrDefault(a => a.FullName?.StartsWith(name) == true);

        var fileProvidersAsm = getAssembly("Microsoft.Extensions.FileProviders.Abstractions");
        var ifileProviderType = fileProvidersAsm?.GetType("Microsoft.Extensions.FileProviders.IFileProvider");
        var ifileInfoType = fileProvidersAsm?.GetType("Microsoft.Extensions.FileProviders.IFileInfo");
        var IFileProvider_GetFileInfoMethod = ifileProviderType?.GetMethod("GetFileInfo");
        var IFileInfo_ExistsPropertyGetter = ifileInfoType?.GetProperty("Exists")?.GetGetMethod();
        var IFileInfo_PhysicalPathPropertyGetter = ifileInfoType?.GetProperty("PhysicalPath")?.GetGetMethod();

        var staticFilesAsm = getAssembly("Microsoft.AspNetCore.StaticFiles");
        var staticFilesOptionsType = staticFilesAsm?.GetType("Microsoft.AspNetCore.Builder.StaticFileOptions");
#pragma warning disable IL2076
        var ioptionsOfStaticFileType = staticFilesOptionsType is not null ? typeof(IOptions<>).MakeGenericType(staticFilesOptionsType) : null;
#pragma warning restore IL2076
        var ioptionsOfStaticFile = ioptionsOfStaticFileType is not null ? services.GetService(ioptionsOfStaticFileType) : null;
        var staticFilesOptions = ioptionsOfStaticFileType?.GetProperty("Value")?.GetValue(ioptionsOfStaticFile);

        var fileProvider = staticFilesOptionsType?.GetProperty("FileProvider")?.GetValue(staticFilesOptions);

        if (fileProvider is null)
        {
            var hostingAsm = getAssembly("Microsoft.AspNetCore.Hosting.Abstractions");
            var webHostEnvType = hostingAsm?.GetType("Microsoft.AspNetCore.Hosting.IWebHostEnvironment");
            var webHostEnv = webHostEnvType is not null ? services.GetService(webHostEnvType) : null;
            fileProvider = webHostEnvType?.GetProperty("WebRootFileProvider")?.GetValue(webHostEnv);
        }

        var IFileProvider_GetFileInfo =
            ifileInfoType is null || IFileProvider_GetFileInfoMethod is null ? ((_) => null) :
            (Func<string, object?>)Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(string), ifileInfoType), fileProvider, IFileProvider_GetFileInfoMethod);

        var IFileInfo_GetExists =
            ifileInfoType is null || IFileInfo_ExistsPropertyGetter is null ? (object _) => false :
            Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(ifileInfoType, typeof(bool)), IFileInfo_ExistsPropertyGetter);

        var IFileInfo_GetPhysicalPath =
            ifileInfoType is null || IFileInfo_PhysicalPathPropertyGetter is null ? (object _) => default(object) :
            Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(ifileInfoType, typeof(string)), IFileInfo_PhysicalPathPropertyGetter);

        return fileProvider is null ? (string _) => null : (string url) =>
        {
            var fileInfo = IFileProvider_GetFileInfo(url);
            if (fileInfo is null) return null;
            var exists = IFileInfo_GetExists.DynamicInvoke(fileInfo) as bool? ?? false;
            if (!exists) return null;
            var physicalPath = IFileInfo_GetPhysicalPath.DynamicInvoke(fileInfo) as string;
            return physicalPath;
        };
    }

    public async ValueTask<Dictionary<string, string>?> ReadAsync(string jsonUrl, string textTableHash)
    {
        var jsonLocalPath = this._getPhysicalPath(jsonUrl);
        jsonLocalPath ??= new Uri(this._baseUri, relativeUri: jsonUrl).LocalPath;
        if (File.Exists(jsonLocalPath))
        {
            var jsonText = await File.ReadAllTextAsync(jsonLocalPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>?>(jsonText);
        }
        return default;
    }
}
