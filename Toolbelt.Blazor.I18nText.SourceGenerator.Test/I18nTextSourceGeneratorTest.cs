using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.SourceGenerator;
using Toolbelt.Blazor.I18nText.SourceGenerator.Test.Internals;
namespace Toolbelt.Blazor.I18nText.CompileTask.Test;

public class I18nTextSourceGeneratorTest
{
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
        this.ValidateGeneratedCSharpCode(fallbackLang, "l47c0gpbnx", fooBarClassSource, fooBarClassName, new[] { "HelloWorld", "Exit", "GreetingOfJA" });
        this.ValidateGeneratedCSharpCode(fallbackLang, "o246f7as05", fizzBuzzClassSource, fizzBuzzClassName, new[] { "Text1", "Text2" });
    }

    private void ValidateGeneratedCSharpCode(string langCode, string hashCode, GeneratedSourceText generatedSource, string generatedClassName, string[] generatedFieldNames)
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