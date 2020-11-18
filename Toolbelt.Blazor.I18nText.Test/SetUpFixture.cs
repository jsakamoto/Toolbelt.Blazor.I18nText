using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Test.Internals;
using static Toolbelt.Blazor.I18nText.Test.Internals.Shell;

namespace Toolbelt.Blazor.I18nText.Test
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            UpdateLibraryProjects();
            UpdatePackageRef();
        }

        private static void UpdateLibraryProjects()
        {
            var testDir = WorkSpace.GetTestDir();
            foreach (var libName in new[] { "Lib4PackRef", "Lib4ProjRef" })
            {
                var libDir = Path.Combine(testDir, libName);
                var libProjPath = Path.Combine(libDir, libName + ".csproj");
                var libProjDocBefore = XDocument.Load(libProjPath);
                var i18textVersionBefore = libProjDocBefore.Descendants("PackageReference").Where(node => node.Attribute("Include").Value == "Toolbelt.Blazor.I18nText").First().Attribute("Version").Value;

                Run(libDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText").ExitCode.Is(0);

                var libProjDocAfter = XDocument.Load(libProjPath);
                var i18textVersionAfter = libProjDocAfter.Descendants("PackageReference").Where(node => node.Attribute("Include").Value == "Toolbelt.Blazor.I18nText").First().Attribute("Version").Value;
                if (i18textVersionBefore != i18textVersionAfter)
                {
                    var libVerNode = libProjDocAfter.Descendants("Version").First();
                    var libCurVer = Version.Parse(libVerNode.Value);
                    var libNextVer = $"{libCurVer.ToString(3)}.{libCurVer.Revision + 1}";
                    libVerNode.Value = libNextVer;
                    libProjDocAfter.Save(libProjPath);
                }

                Run(libDir, "dotnet", "build").ExitCode.Is(0);
            }
        }

        private static void UpdatePackageRef()
        {
            var testDir = WorkSpace.GetTestDir();
            var projectNames = new[] { "Components", "Client", "Server" };
            foreach (var projectName in projectNames)
            {
                var projectDir = Path.Combine(testDir, projectName);
                Run(projectDir, "dotnet", "add", "package", "Toolbelt.Blazor.I18nText").ExitCode.Is(0);
                if (projectName == "Components")
                {
                    Run(projectDir, "dotnet", "add", "package", "Lib4PackRef").ExitCode.Is(0);
                }
            }
        }
    }
}
