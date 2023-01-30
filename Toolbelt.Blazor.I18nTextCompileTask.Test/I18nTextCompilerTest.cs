using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;
using Toolbelt.Blazor.I18nText.Test.Internals;

namespace Toolbelt.Blazor.I18nText.Test;

[Parallelizable(ParallelScope.All)]
public class I18nTextCompilerTest
{
    [Test]
    public void GenerateHash_Test()
    {
        /*
        enGreetingHello, World!HomeHomefrGreetingBonjour le monde!
        sha256 = 660449602e0f75ed3002e1ef5e83f2e89a35b71de12aa0f569f8602993ec5915
        base36 = ua6i0t8k6n
         */
        var i18ntext = new I18nTextType();
        i18ntext.Langs.TryAdd("en", new I18nTextTable(new Dictionary<string, string>
        {
            {"Greeting", "Hello, World!" },
            {"Home", "Home" }
        }));
        i18ntext.Langs.TryAdd("fr", new I18nTextTable(new Dictionary<string, string>
        {
            {"Greeting", "Bonjour le monde!" }
        }));

        var hash = I18nTextCompiler.GenerateHash(i18ntext);
        hash.Is("ua6i0t8k6n");
    }

    /// <summary>
    /// Compile - I18n Text Typed Class was generated
    /// </summary>
    [TestCase("en", true)]
    [TestCase("en-us", false)]
    [TestCase("ja", false)]
    [TestCase("ja-jp", true)]
    public void Compile_I18nTextTypedClassWasGenerated_Test(string langCode, bool disableSubNameSpace)
    {
        using var workSpace = new WorkSpace();

        var srcFiles = "*.json;*.csv".Split(';')
            .SelectMany(pattern => Directory.GetFiles(workSpace.I18nTextDir, pattern, SearchOption.AllDirectories))
            .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir)
        {
            FallBackLanguage = langCode,
            OutDirectory = workSpace.TextResJsonsDir,
            DisableSubNameSpace = disableSubNameSpace
        };
        var compiler = new I18nTextCompiler();
        var success = compiler.Compile(srcFiles, options);
        success.IsTrue();

        // Compiled an i18n text type file should exist.
        const string nameSpace = "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.";
        var fooBarClass = nameSpace + "Foo.Bar";
        var fizzBuzzClass = nameSpace + (disableSubNameSpace ? "Buzz" : "Fizz.Buzz");
        var csFileNames = new[] { fooBarClass + ".cs", fizzBuzzClass + ".cs" }.OrderBy(n => n);
        Directory.Exists(workSpace.TypesDir).IsTrue();
        Directory.GetFiles(workSpace.TypesDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(n => n)
            .Is(csFileNames);

        // the i18n text type file should be valid C# code.
        this.ValidateGeneratedCSharpCode(workSpace, langCode, "l47c0gpbnx", fooBarClass + ".cs", fooBarClass, new[] { "HelloWorld", "Exit", "GreetingOfJA" });
        this.ValidateGeneratedCSharpCode(workSpace, langCode, "o246f7as05", fizzBuzzClass + ".cs", fizzBuzzClass, new[] { "Text1", "Text2" });
    }

    private void ValidateGeneratedCSharpCode(WorkSpace workSpace, string langCode, string hashCode, string csFileName, string generatedClassName, string[] generatedFieldNames)
    {
        // the i18n text type file should be valid C# code.
        var typeCode = File.ReadAllText(Path.Combine(workSpace.TypesDir, csFileName));
        var syntaxTree = CSharpSyntaxTree.ParseText(typeCode);
        var assemblyName = Path.GetRandomFileName();
        var references = new[] {
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
            MetadataReference.CreateFromFile(typeof(I18nTextFallbackLanguage).GetTypeInfo().Assembly.Location),
        };
        var compilation = CSharpCompilation.Create(
           assemblyName,
           syntaxTrees: new[] { syntaxTree },
           references: references,
           options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compiledType = default(Type);
        using (var ms = new MemoryStream())
        {
            var result = compilation.Emit(ms);
            result.Success.IsTrue();

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
            compiledType = assembly.GetType(generatedClassName);
        }

        // the i18n text type file should contain the i18n text typed public class.
        compiledType.IsNotNull();
        compiledType.IsClass.IsTrue();
        compiledType.IsPublic.IsTrue();

        // the i18n text typed class has fileds that are combined all languages files.
        var fields = compiledType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var generatedFieldName in generatedFieldNames)
        {
            fields.Where(f => f.FieldType == typeof(string)).Any(f => f.Name == generatedFieldName).IsTrue();
        }

        var textTableObj = Activator.CreateInstance(compiledType);
        textTableObj.IsNotNull();
        textTableObj.IsInstanceOf<I18nTextFallbackLanguage>().FallBackLanguage.Is(langCode);
        foreach (var generatedFieldName in generatedFieldNames)
        {
            textTableObj.IsInstanceOf<I18nTextLateBinding>()[generatedFieldName].Is(generatedFieldName);
        }

        // the i18n text typed class has the filed that is represent its hash code.
        textTableObj.IsInstanceOf<I18nTextTableHash>().Hash.Is(hashCode);
    }

    /// <summary>
    /// Compile - I18n Text JSON files were generated
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void Compile_I18nTextJsonFilesWereGenerated_Test(bool disableSubNameSpace)
    {
        using var workSpace = new WorkSpace();

        var srcFiles = "*.json;*.csv".Split(';')
            .SelectMany(pattern => Directory.GetFiles(workSpace.I18nTextDir, pattern, SearchOption.AllDirectories))
            .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir)
        {
            OutDirectory = workSpace.TextResJsonsDir,
            DisableSubNameSpace = disableSubNameSpace
        };
        var compiler = new I18nTextCompiler();
        var success = compiler.Compile(srcFiles, options);
        success.IsTrue();

        // Compiled i18n text json files should exist.
        Directory.Exists(workSpace.TextResJsonsDir).IsTrue();
        Directory.GetFiles(workSpace.TextResJsonsDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .Is(new[]{
                $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.{(disableSubNameSpace ? "" : "Fizz.")}Buzz.en.json",
                $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.{(disableSubNameSpace ? "" : "Fizz.")}Buzz.ja.json",
                $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json",
                $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json" }.OrderBy(n => n));

        var enJsonText = File.ReadAllText(Path.Combine(workSpace.TextResJsonsDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json"));
        var enTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(enJsonText) ?? new();
        enTexts["HelloWorld"].Is("Hello World!");
        enTexts["Exit"].Is("Exit");
        enTexts["GreetingOfJA"].Is("こんにちは");

        var jaJsonText = File.ReadAllText(Path.Combine(workSpace.TextResJsonsDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json"));
        var jaTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(jaJsonText) ?? new();
        jaTexts["HelloWorld"].Is("こんにちは世界!");
        jaTexts["Exit"].Is("Exit");
        jaTexts["GreetingOfJA"].Is("こんにちは");
    }

    /// <summary>
    /// Compile - No Source Files
    /// </summary>
    [Test]
    public void Compile_NoSrcFiles_Test()
    {
        using var workSpace = new WorkSpace();
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir);
        var compiler = new I18nTextCompiler();
        var success = compiler.Compile(Enumerable.Empty<I18nTextSourceFile>(), options);

        success.IsTrue();
        Directory.Exists(workSpace.TypesDir).IsFalse();
        Directory.Exists(workSpace.TextResJsonsDir).IsFalse();
    }

    /// <summary>
    /// Compile - Error by fallback lang not exist
    /// </summary>
    [Test]
    public void Compile_Error_FallbackLangNotExist()
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
        var compiler = new I18nTextCompiler();
        var suceess = compiler.Compile(srcFiles, options);

        suceess.IsFalse();
        logErrors.Count.Is(1);
        logErrors.First()
            .StartsWith("Could not find an I18n source text file of fallback language 'fr', for 'Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.")
            .IsTrue();
    }

    /// <summary>
    /// Compile - Error by an invalid format json file
    /// </summary>
    [Test]
    public void Compile_Error_InvalidJsonFormat()
    {
        using var workSpace = new WorkSpace("i18ntext (I18N002 error)");

        var srcFile = new TaskItem(Path.Combine(workSpace.I18nTextDir, "Localized.en.json"));

        var buildEngine = new BuildEngine();
        var compileI18nTextMSBuildTask = new CompileI18nText
        {
            BuildEngine = buildEngine,
            FallBackLanguage = "en",
            BaseDir = workSpace.ProjectDir,
            I18nTextSourceDirectory = workSpace.I18nTextDir,
            TypesDirectory = workSpace.TypesDir,
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
    /// Compile - sweep type files
    /// </summary>
    [Test]
    public void Compile_SweepTypeFiles_Test()
    {
        using var workSpace = new WorkSpace();

        if (Directory.Exists(workSpace.TypesDir)) Directory.Delete(workSpace.TypesDir, recursive: true);
        Directory.CreateDirectory(workSpace.TypesDir);
        File.WriteAllLines(Path.Combine(workSpace.TypesDir, "Bar.cs"), new[] { "// <auto-generated by=\"the Blazor I18n Text compiler\" />" });
        File.WriteAllLines(Path.Combine(workSpace.TypesDir, "Bar.cs.bak"), new[] { "// <auto-generated by=\"the Blazor I18n Text compiler\" />" });
        File.WriteAllLines(Path.Combine(workSpace.TypesDir, "Fizz.cs"), new[] { "public class Fizz {}" });

        var srcPath = Path.Combine(workSpace.I18nTextDir, "Foo.Bar.en.json");
        var srcFiles = new[] { new I18nTextSourceFile(srcPath, Encoding.UTF8) };
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir) { OutDirectory = workSpace.TextResJsonsDir };
        var compiler = new I18nTextCompiler();
        var suceess = compiler.Compile(srcFiles, options);

        suceess.IsTrue();

        Directory.GetFiles(workSpace.TypesDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .Is(
                // "Bar.cs" should be sweeped.
                "Bar.cs.bak",
                "Fizz.cs",
                "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.cs");

        File.ReadLines(Path.Combine(workSpace.TypesDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.cs"))
            .FirstOrDefault()
            .Is("// <auto-generated by=\"the Blazor I18n Text compiler\" />");
    }

    /// <summary>
    /// Compile - sweep I18n Text JSON files
    /// </summary>
    [Test]
    public void Compile_SweepTextJsonFiles_Test()
    {
        using var workSpace = new WorkSpace();

        if (Directory.Exists(workSpace.TextResJsonsDir)) Directory.Delete(workSpace.TextResJsonsDir, recursive: true);
        Directory.CreateDirectory(workSpace.TextResJsonsDir);
        File.WriteAllLines(Path.Combine(workSpace.TextResJsonsDir, "Bar.json"), new[] { "{\"Key\":\"Value\"}" });

        var srcPath = Path.Combine(workSpace.I18nTextDir, "Foo.Bar.en.json");
        var srcFiles = new[] { new I18nTextSourceFile(srcPath, Encoding.UTF8) };
        var options = new I18nTextCompilerOptions(workSpace.ProjectDir) { OutDirectory = workSpace.TextResJsonsDir };
        var compiler = new I18nTextCompiler();
        var suceess = compiler.Compile(srcFiles, options);

        suceess.IsTrue();

        // "Bar.json" should be sweeped.
        Directory.GetFiles(workSpace.TextResJsonsDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .Is("Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json");
    }
}
