using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Toolbelt.Blazor.I18nText.Interfaces;
using Xunit;

namespace Toolbelt.Blazor.I18nText.Test
{
    public class I18nTextCompilerTest : IDisposable
    {
        private string _OriginalCurrentDir;

        private string _TypesDir;

        private string _WwwRootDir;

        public I18nTextCompilerTest()
        {
            _OriginalCurrentDir = Environment.CurrentDirectory;
            while (!Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").Any())
                Environment.CurrentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
            _TypesDir = Path.Combine(Environment.CurrentDirectory, "i18ntext", "@types");
            _WwwRootDir = Path.Combine(Environment.CurrentDirectory, "wwwroot");
        }

        public void Dispose()
        {
            if (Directory.Exists(_TypesDir)) Directory.Delete(_TypesDir, recursive: true);
            if (Directory.Exists(_WwwRootDir)) Directory.Delete(_WwwRootDir, recursive: true);
            Environment.CurrentDirectory = _OriginalCurrentDir;
        }

        [Theory(DisplayName = "Compile - I18n Text Typed Class was generated")]
        [InlineData("en")]
        [InlineData("en-us")]
        [InlineData("ja")]
        [InlineData("ja-jp")]
        public void Compile_I18nTextTypedClassWasGenerated_Test(string langCode)
        {
            var srcFilePath = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "i18ntext"), "*.json", SearchOption.AllDirectories);
            var options = new I18nTextCompilerOptions { FallBackLanguage = langCode };
            var compiler = new I18nTextCompiler();
            var success = compiler.Compile(srcFilePath, options);
            success.IsTrue();

            // Compiled an i18n text type file should exist.
            var csFileName = "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.cs";
            Directory.Exists(_TypesDir).IsTrue();
            Directory.GetFiles(_TypesDir).Select(path => Path.GetFileName(path)).Is(csFileName);

            // the i18n text type file should be valid C# code.
            var typeCode = File.ReadAllText(Path.Combine(_TypesDir, csFileName));
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
                compiledType = assembly.GetType("Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar");
            }

            // the i18n text type file should contain the i18n text typed public class.
            compiledType.IsNotNull();
            compiledType.IsClass.IsTrue();
            compiledType.IsPublic.IsTrue();

            // the i18n text typed class has fileds that are combined all languages files.
            var fields = compiledType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            fields.Where(f => f.FieldType == typeof(string)).Any(f => f.Name == "HelloWorld").IsTrue();
            fields.Where(f => f.FieldType == typeof(string)).Any(f => f.Name == "Exit").IsTrue();
            fields.Where(f => f.FieldType == typeof(string)).Any(f => f.Name == "GreetingOfJA").IsTrue();

            var textTableObj = Activator.CreateInstance(compiledType);
            textTableObj.IsNotNull();
            (textTableObj as I18nTextFallbackLanguage).FallBackLanguage.Is(langCode);
            (textTableObj as I18nTextLateBinding)["HelloWorld"].Is("HelloWorld");
        }

        [Fact(DisplayName = "Compile - I18n Text JSON files were generated")]
        public void Compile_I18nTextJsonFilesWereGenerated_Test()
        {
            var srcFilePath = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "i18ntext"), "*.json", SearchOption.AllDirectories);
            var options = new I18nTextCompilerOptions();
            var compiler = new I18nTextCompiler();
            var success = compiler.Compile(srcFilePath, options);
            success.IsTrue();

            // Compiled i18n text json files should exist.
            var jsonDir = Path.Combine(_WwwRootDir, "content", "i18ntext");
            Directory.Exists(jsonDir).IsTrue();
            Directory.GetFiles(jsonDir)
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name)
                .Is("Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json",
                    "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json");

            var enJsonText = File.ReadAllText(Path.Combine(jsonDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json"));
            var enTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(enJsonText);
            enTexts["HelloWorld"].Is("Hello World!");
            enTexts["Exit"].Is("Exit");
            enTexts["GreetingOfJA"].Is("こんにちは");

            var jaJsonText = File.ReadAllText(Path.Combine(jsonDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json"));
            var jaTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(jaJsonText);
            jaTexts["HelloWorld"].Is("こんにちは世界!");
            jaTexts["Exit"].Is("Exit");
            jaTexts["GreetingOfJA"].Is("こんにちは");
        }

        [Fact(DisplayName = "Compile - No Source Files")]
        public void Compile_NoSrcFiles_Test()
        {
            var options = new I18nTextCompilerOptions();
            var compiler = new I18nTextCompiler();
            var success = compiler.Compile(new string[] { }, options);

            success.IsTrue();
            Directory.Exists(_TypesDir).IsFalse();
            Directory.Exists(_WwwRootDir).IsFalse();
        }

        [Fact(DisplayName = "Compile - Error by fallback lang not exist")]
        public void Compile_Error_FallbackLangNotExist()
        {
            var srcFilePath = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "i18ntext"), "*.json", SearchOption.AllDirectories);
            var logErr = new List<string>();
            var options = new I18nTextCompilerOptions
            {
                FallBackLanguage = "fr",
                LogError = msg => logErr.Add(msg)
            };
            var compiler = new I18nTextCompiler();
            var suceess = compiler.Compile(srcFilePath, options);

            suceess.IsFalse();
            logErr.Is("IN1001: Could not find an I18n source text file of fallback language 'fr', for 'Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar'.");
        }
    }
}
