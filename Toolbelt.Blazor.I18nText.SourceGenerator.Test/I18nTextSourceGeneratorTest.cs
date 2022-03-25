using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;
using Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test;

public class I18nTextSourceGeneratorTest
{
    /// <summary>
    /// Compile - I18n Text Typed Class was generated
    /// </summary>
    [TestCase("en", true)]
    [TestCase("en-us", false)]
    [TestCase("ja", false)]
    [TestCase("ja-jp", true)]
    public void Compile_I18nTextTypedClassesWereGenerated_Test(string fallbackLang, bool disableSubNameSpace)
    {
        // Given
        using var workSpace = new WorkSpace();
        var context = workSpace.CreateGeneratorExecutionContext(
            fallbackLang: fallbackLang,
            disableSubNameSpace: disableSubNameSpace);

        // When
        new I18nTextSourceGenerator().Execute(context);

        // Then: Validate compiled to i18n text types
        var generatedSourceTexts = context.GetGeneratedSourceTexts();
        const string nameSpace = "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText";
        var fooBarClassName = $"{nameSpace}.Foo.Bar";
        var fizzBuzzClassName = disableSubNameSpace ? $"{nameSpace}.Buzz" : $"{nameSpace}.Fizz.Buzz";

        var fooBarClassSource = generatedSourceTexts.First(t => t.HintName == $"{fooBarClassName}.g.cs");
        var fizzBuzzClassSource = generatedSourceTexts.First(t => t.HintName == $"{fizzBuzzClassName}.g.cs");

        // The generated i18n text type source should be a valid C# code.
        ValidateGeneratedCSharpCode(fallbackLang, "l47c0gpbnx", fooBarClassSource, fooBarClassName, new[] { "HelloWorld", "Exit", "GreetingOfJA" });
        ValidateGeneratedCSharpCode(fallbackLang, "o246f7as05", fizzBuzzClassSource, fizzBuzzClassName, new[] { "Text1", "Text2" });

        // and, there are no errors.
        context.GetDiagnostics().Any().IsFalse();
    }

    /// <summary>
    /// Compile - I18n Text JSON files were generated
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void Compile_I18nTextJsonFilesWereGenerated_Test(bool disableSubNameSpace)
    {
        // Given
        using var workSpace = new WorkSpace();
        var context = workSpace.CreateGeneratorExecutionContext(disableSubNameSpace: disableSubNameSpace);

        // When
        new I18nTextSourceGenerator().Execute(context);

        // Then: Compiled i18n text json files should exist.
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

        var enTexts = JsonSerializer.Deserialize<Dictionary<string, string>>(enJsonText)!;
        enTexts["HelloWorld"].Is("Hello World!");
        enTexts["Exit"].Is("Exit");
        enTexts["GreetingOfJA"].Is("Ç±ÇÒÇ…ÇøÇÕ");

        var jaJsonText = File.ReadAllText(Path.Combine(workSpace.TextResJsonsDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json"));
        var jaTexts = JsonSerializer.Deserialize<Dictionary<string, string>>(jaJsonText)!;
        jaTexts["HelloWorld"].Is("Ç±ÇÒÇ…ÇøÇÕê¢äE!");
        jaTexts["Exit"].Is("Exit");
        jaTexts["GreetingOfJA"].Is("Ç±ÇÒÇ…ÇøÇÕ");

        // and, there are no errors.
        context.GetDiagnostics().Any().IsFalse();
    }

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
    /// Compile - No Source Files
    /// </summary>
    [Test]
    public void Compile_NoSrcFiles_Test()
    {
        // Given
        using var workSpace = new WorkSpace();
        var context = workSpace.CreateGeneratorExecutionContext(
            filterAdditionalFiles: _ => false // Make the additional files to be empty.
        );

        // When
        new I18nTextSourceGenerator().Execute(context);

        // Then: Nothing to be generated, and any errors never occurred.
        context.GetGeneratedSourceTexts().Any().IsFalse();
        Directory.Exists(workSpace.TextResJsonsDir).IsFalse();
        context.GetDiagnostics().Any().IsFalse();
    }

    /// <summary>
    /// Compile - Error by fallback lang not exist
    /// </summary>
    [Test]
    public void Compile_Error_FallbackLangNotExist()
    {
        // Given
        using var workSpace = new WorkSpace();
        var context = workSpace.CreateGeneratorExecutionContext(
            fallbackLang: "fr",
            // Make the additional files to be only "Foo.Bar.{en|ja}.{json|csv}" files to stabilize this test.
            filterAdditionalFiles: file => Path.GetFileName(file.Path).StartsWith("Foo.Bar."));

        // When
        new I18nTextSourceGenerator().Execute(context);

        // Then: It souhld be error.
        context.GetDiagnostics().Select(d => d.ToString()).Is(
            "error I18N001: Could not find an I18n source text file of fallback language 'fr', for 'Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar'.");
    }

    /// <summary>
    /// Compile - sweep I18n Text JSON files
    /// </summary>
    [Test]
    public void Compile_SweepTextJsonFiles_Test()
    {
        // Given
        using var workSpace = new WorkSpace();
        var context = workSpace.CreateGeneratorExecutionContext(
            fallbackLang: "ja",
            // Make the additional files to be only one to make this test to be simpler.
            filterAdditionalFiles: file => Path.GetFileName(file.Path) == "Foo.Bar.ja.csv"
        );
        Directory.CreateDirectory(workSpace.TextResJsonsDir);
        File.WriteAllLines(Path.Combine(workSpace.TextResJsonsDir, "Bar.json"), new[] { "{\"Key\":\"Value\"}" });

        // When
        new I18nTextSourceGenerator().Execute(context);

        // Then: "Bar.json" should be sweeped.
        Directory.GetFiles(workSpace.TextResJsonsDir)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .Is("Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json");
    }

    private static void ValidateGeneratedCSharpCode(string langCode, string hashCode, GeneratedSourceText generatedSource, string generatedClassName, string[] generatedFieldNames)
    {
        // The generated i18n text type source should be a valid C# code.
        var typeCode = generatedSource.Text.ToString();
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
        compiledType!.IsClass.IsTrue();
        compiledType.IsPublic.IsTrue();

        // the i18n text typed class has fileds that are combined all languages files.
        var fields = compiledType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var generatedFieldName in generatedFieldNames)
        {
            fields.Where(f => f.FieldType == typeof(string)).Any(f => f.Name == generatedFieldName).IsTrue();
        }

        var textTableObj = Activator.CreateInstance(compiledType);
        textTableObj.IsNotNull();
        (textTableObj as I18nTextFallbackLanguage)!.FallBackLanguage.Is(langCode);
        foreach (var generatedFieldName in generatedFieldNames)
        {
            (textTableObj as I18nTextLateBinding)![generatedFieldName].Is(generatedFieldName);
        }

        // the i18n text typed class has the filed that is represent its hash code.
        (textTableObj as I18nTextTableHash)!.Hash.Is(hashCode);
    }
}