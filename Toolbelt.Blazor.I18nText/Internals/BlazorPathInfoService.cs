using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class BlazorPathInfoService
    {
        private bool Initialized = false;

        private BlazorPathInfo BlazorPathInfo;

        public BlazorPathInfo GetPathInfo(Type typeOfStartUp)
        {
            lock (this)
            {
                if (!Initialized)
                {
                    Initialized = true;

                    var typeOfBlazorConfig = Type.GetType("Microsoft.AspNetCore.Blazor.Server.BlazorConfig, Microsoft.AspNetCore.Blazor.Server");
                    if (typeOfBlazorConfig == null) return null;

                    var readMethod = typeOfBlazorConfig.GetMethod("Read", BindingFlags.Static | BindingFlags.Public);
                    var webRootPathProp = typeOfBlazorConfig.GetProperty("WebRootPath", BindingFlags.Instance | BindingFlags.Public);
                    var distPathProp = typeOfBlazorConfig.GetProperty("DistPath", BindingFlags.Instance | BindingFlags.Public);
                    if (readMethod == null) return null;
                    if (webRootPathProp == null) return null;
                    if (distPathProp == null) return null;

                    var blazorConfig = readMethod.Invoke(null, new object[] { typeOfStartUp.Assembly.Location });
                    if (blazorConfig == null) return null;

                    var webRootPath = webRootPathProp.GetValue(blazorConfig, null) as string;
                    var distPath = distPathProp.GetValue(blazorConfig, null) as string;

                    var webRootDir = new[] { webRootPath, distPath }
                        .Where(path => !string.IsNullOrEmpty(path))
                        .Where(path => Directory.Exists(path))
                        .FirstOrDefault();
                    if (string.IsNullOrEmpty(webRootDir)) return null;

                    if (!webRootDir.EndsWith(Path.DirectorySeparatorChar.ToString())) webRootDir += Path.DirectorySeparatorChar;
                    if (!distPath.EndsWith(Path.DirectorySeparatorChar.ToString())) distPath += Path.DirectorySeparatorChar;

                    this.BlazorPathInfo = new BlazorPathInfo(webRoot: new Uri(webRootDir), dist: new Uri(distPath));
                }
                return this.BlazorPathInfo;
            }
        }
    }
}
