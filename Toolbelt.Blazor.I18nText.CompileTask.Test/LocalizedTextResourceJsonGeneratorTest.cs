using System.Text;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Compiler.Shared;
using Toolbelt.Blazor.I18nText.CompilerTask;
using Toolbelt.Blazor.I18nText.CompileTask.Test.Internals;

namespace Toolbelt.Blazor.I18nText.CompileTask.Test;

[Parallelizable(ParallelScope.All)]
public class LocalizedTextResourceJsonGeneratorTest
{
    /// <summary>
    /// Generate - Localized Text Resource JSON files were generated
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void Generate_I18nTextResourceJsonFilesWereGenerated_Test(bool disableSubNameSpace)
    {
        // Given
        using var workSpace = new WorkSpace();

        var srcFiles = "*.json;*.csv".Split(';')
            .SelectMany(pattern => Directory.GetFiles(workSpace.I18nTextDir, pattern, SearchOption.AllDirectories))
            .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir)
        {
            OutDirectory = workSpace.TextResJsonsDir,
            DisableSubNameSpace = disableSubNameSpace,
            LogError = e => Assert.Fail(e.Message)
        };

        // When
        var generator = new LocalizedTextResourceJsonGenerator();
        var success = generator.Generate(srcFiles, options, CancellationToken.None);

        // Then: Compiled i18n text json files should exist.
        success.IsTrue();
        Directory.Exists(workSpace.TextResJsonsDir).IsTrue();
        Directory.GetFiles(workSpace.TextResJsonsDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .Is(new[]{
                $"Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.{(disableSubNameSpace ? "" : "Fizz.")}Buzz.en.json",
                $"Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.{(disableSubNameSpace ? "" : "Fizz.")}Buzz.ja.json",
                $"Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.Foo.Bar.en.json",
                $"Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.Foo.Bar.ja.json" }.OrderBy(n => n));

        var enJsonText = File.ReadAllText(Path.Combine(workSpace.TextResJsonsDir, "Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.Foo.Bar.en.json"));
        var enTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(enJsonText) ?? new();
        enTexts["HelloWorld"].Is("Hello World!");
        enTexts["Exit"].Is("Exit");
        enTexts["GreetingOfJA"].Is("こんにちは");

        var jaJsonText = File.ReadAllText(Path.Combine(workSpace.TextResJsonsDir, "Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.Foo.Bar.ja.json"));
        var jaTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(jaJsonText) ?? new();
        jaTexts["HelloWorld"].Is("こんにちは世界!");
        jaTexts["Exit"].Is("Exit");
        jaTexts["GreetingOfJA"].Is("こんにちは");
    }

    /// <summary>
    /// Generate - No Source Files
    /// </summary>
    [Test]
    public void Generate_NoSrcFiles_Test()
    {
        using var workSpace = new WorkSpace();
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir);
        var generator = new LocalizedTextResourceJsonGenerator();
        var success = generator.Generate(Enumerable.Empty<I18nTextSourceFile>(), options, CancellationToken.None);

        success.IsTrue();
        Directory.Exists(workSpace.TextResJsonsDir).IsFalse();
    }

    /// <summary>
    /// Generate - Error by fallback lang not exist
    /// </summary>
    [Test]
    public void Generate_Error_FallbackLangNotExist()
    {
        using var workSpace = new WorkSpace();

        var srcFiles = Directory.GetFiles(workSpace.I18nTextDir, "*.json", SearchOption.AllDirectories)
            .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
        var logErrors = new List<string>();
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir)
        {
            FallBackLanguage = "fr",
            LogError = e => logErrors.Add(e.Message),
            OutDirectory = workSpace.TextResJsonsDir
        };
        var generator = new LocalizedTextResourceJsonGenerator();
        var success = generator.Generate(srcFiles, options, CancellationToken.None);

        success.IsFalse();
        logErrors.Count.Is(1);
        logErrors.First()
            .StartsWith("Could not find a localized text source file of fallback language 'fr', for 'Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.")
            .IsTrue();
    }

    /// <summary>
    /// Generate - Error by an invalid format json file
    /// </summary>
    [Test]
    public void Generate_Error_InvalidJsonFormat()
    {
        using var workSpace = new WorkSpace("i18ntext (I18N002 error)");

        var srcFile = new TaskItem(Path.Combine(workSpace.I18nTextDir, "Localized.en.json"));

        var buildEngine = new BuildEngine();
        var compileI18nTextMSBuildTask = new GenerateLocalizedTextResourceJson
        {
            BuildEngine = buildEngine,
            FallBackLanguage = "en",
            BaseDir = workSpace.ProjectDir,
            I18nTextSourceDirectory = workSpace.I18nTextDir,
            OutDirectory = workSpace.TextResJsonsDir,
            NameSpace = "Test",
            Include = new[] { srcFile }
        };

        var success = compileI18nTextMSBuildTask.Execute();

        success.IsFalse();
        buildEngine.LoggedBuildErrors.Count().Is(1);
        var loggedError = buildEngine.LoggedBuildErrors.First();
        loggedError.Code.Is("IN002");
        loggedError.Message.Is("After parsing a value an unexpected character was encountered: \". Path 'key3', line 5, position 2.");
        loggedError.File.Is(srcFile.ItemSpec);
        loggedError.LineNumber.Is(5);
        loggedError.EndLineNumber.Is(5);
        loggedError.ColumnNumber.Is(2);
        loggedError.EndColumnNumber.Is(2);
    }

    /// <summary>
    /// Generate - sweep Localized Text Resource JSON files
    /// </summary>
    [Test]
    public void Generate_SweepTextJsonFiles_Test()
    {
        // Given
        using var workSpace = new WorkSpace();

        if (Directory.Exists(workSpace.TextResJsonsDir)) Directory.Delete(workSpace.TextResJsonsDir, recursive: true);
        Directory.CreateDirectory(workSpace.TextResJsonsDir);
        File.WriteAllLines(Path.Combine(workSpace.TextResJsonsDir, "Bar.json"), ["{\"Key\":\"Value\"}"]);

        var srcPath = Path.Combine(workSpace.I18nTextDir, "Foo.Bar.en.json");
        var srcFiles = new[] { new I18nTextSourceFile(srcPath, Encoding.UTF8) };
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir) { OutDirectory = workSpace.TextResJsonsDir };

        // When
        var generator = new LocalizedTextResourceJsonGenerator();
        var success = generator.Generate(srcFiles, options, CancellationToken.None);

        // Then: "Bar.json" should be swept.
        success.IsTrue();
        Directory.GetFiles(workSpace.TextResJsonsDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .Is("Toolbelt.Blazor.I18nText.CompileTask.Test.I18nText.Foo.Bar.en.json");
    }
}
