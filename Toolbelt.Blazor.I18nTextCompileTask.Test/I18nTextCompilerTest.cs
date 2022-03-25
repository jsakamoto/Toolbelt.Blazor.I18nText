using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Toolbelt.Blazor.I18nText.Interfaces;
using Toolbelt.Blazor.I18nText.Internals;
using Xunit;

namespace Toolbelt.Blazor.I18nText.Test
{
    public class I18nTextCompilerTest : IDisposable
    {
        private string _OriginalCurrentDir;

        private string _TypesDir;

        private string _TextResJsonsDir;

        public I18nTextCompilerTest()
        {
            this._OriginalCurrentDir = Environment.CurrentDirectory;
            while (!Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").Any())
                Environment.CurrentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
            this._TypesDir = Path.Combine(Environment.CurrentDirectory, "i18ntext", "@types");
            this._TextResJsonsDir = Path.Combine(Environment.CurrentDirectory, "obj", "Debug", "netstandard2.0", "dist", "_content", "i18ntext");
        }

        public void Dispose()
        {
            if (Directory.Exists(this._TypesDir)) Directory.Delete(this._TypesDir, recursive: true);
            if (Directory.Exists(this._TextResJsonsDir)) Directory.Delete(this._TextResJsonsDir, recursive: true);
            Environment.CurrentDirectory = this._OriginalCurrentDir;
        }

        [Fact]
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

        [Theory(DisplayName = "Compile - I18n Text Typed Class was generated")]
        [InlineData("en", true)]
        [InlineData("en-us", false)]
        [InlineData("ja", false)]
        [InlineData("ja-jp", true)]
        public void Compile_I18nTextTypedClassWasGenerated_Test(string langCode, bool disableSubNameSpace)
        {
            var srcFiles = "*.json;*.csv".Split(';')
                .SelectMany(pattern => Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "i18ntext"), pattern, SearchOption.AllDirectories))
                .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
            var options = new I18nTextCompilerOptions
            {
                FallBackLanguage = langCode,
                OutDirectory = _TextResJsonsDir,
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
            Directory.Exists(this._TypesDir).IsTrue();
            Directory.GetFiles(this._TypesDir)
                .Select(path => Path.GetFileName(path))
                .OrderBy(n => n)
                .Is(csFileNames);

            // the i18n text type file should be valid C# code.
            this.ValidateGeneratedCSharpCode(langCode, "l47c0gpbnx", fooBarClass + ".cs", fooBarClass, new[] { "HelloWorld", "Exit", "GreetingOfJA" });
            this.ValidateGeneratedCSharpCode(langCode, "o246f7as05", fizzBuzzClass + ".cs", fizzBuzzClass, new[] { "Text1", "Text2" });
        }

        private void ValidateGeneratedCSharpCode(string langCode, string hashCode, string csFileName, string generatedClassName, string[] generatedFieldNames)
        {
            // the i18n text type file should be valid C# code.
            var typeCode = File.ReadAllText(Path.Combine(this._TypesDir, csFileName));
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
            (textTableObj as I18nTextFallbackLanguage).FallBackLanguage.Is(langCode);
            foreach (var generatedFieldName in generatedFieldNames)
            {
                (textTableObj as I18nTextLateBinding)[generatedFieldName].Is(generatedFieldName);
            }

            // the i18n text typed class has the filed that is represent its hash code.
            (textTableObj as I18nTextTableHash).Hash.Is(hashCode);
        }

        [Theory(DisplayName = "Compile - I18n Text JSON files were generated")]
        [InlineData(true)]
        [InlineData(false)]
        public void Compile_I18nTextJsonFilesWereGenerated_Test(bool disableSubNameSpace)
        {
            var srcFiles = "*.json;*.csv".Split(';')
                .SelectMany(pattern => Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "i18ntext"), pattern, SearchOption.AllDirectories))
                .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
            var options = new I18nTextCompilerOptions
            {
                OutDirectory = _TextResJsonsDir,
                DisableSubNameSpace = disableSubNameSpace
            };
            var compiler = new I18nTextCompiler();
            var success = compiler.Compile(srcFiles, options);
            success.IsTrue();

            // Compiled i18n text json files should exist.
            Directory.Exists(this._TextResJsonsDir).IsTrue();
            Directory.GetFiles(this._TextResJsonsDir)
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name)
                .Is(new[]{
                    $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.{(disableSubNameSpace ? "" : "Fizz.")}Buzz.en.json",
                    $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.{(disableSubNameSpace ? "" : "Fizz.")}Buzz.ja.json",
                    $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json",
                    $"Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json" }.OrderBy(n => n));

            var enJsonText = File.ReadAllText(Path.Combine(this._TextResJsonsDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json"));
            var enTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(enJsonText);
            enTexts["HelloWorld"].Is("Hello World!");
            enTexts["Exit"].Is("Exit");
            enTexts["GreetingOfJA"].Is("こんにちは");

            var jaJsonText = File.ReadAllText(Path.Combine(this._TextResJsonsDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.ja.json"));
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
            var success = compiler.Compile(Enumerable.Empty<I18nTextSourceFile>(), options);

            success.IsTrue();
            Directory.Exists(this._TypesDir).IsFalse();
            Directory.Exists(this._TextResJsonsDir).IsFalse();
        }

        [Fact(DisplayName = "Compile - Error by fallback lang not exist")]
        public void Compile_Error_FallbackLangNotExist()
        {
            var srcFiles = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "i18ntext"), "*.json", SearchOption.AllDirectories)
                .Select(path => new I18nTextSourceFile(path, Encoding.UTF8));
            var logErrors = new List<string>();
            var options = new I18nTextCompilerOptions
            {
                FallBackLanguage = "fr",
                LogError = ex => logErrors.Add(ex.Message),
                OutDirectory = _TextResJsonsDir
            };
            var compiler = new I18nTextCompiler();
            var suceess = compiler.Compile(srcFiles, options);

            suceess.IsFalse();
            logErrors.Count.Is(1);
            logErrors.First()
                .StartsWith("Could not find an I18n source text file of fallback language 'fr', for 'Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.")
                .IsTrue();
        }

        [Fact(DisplayName = "Compile - sweep type files")]
        public void Compile_SweepTypeFiles_Test()
        {
            if (Directory.Exists(this._TypesDir)) Directory.Delete(this._TypesDir, recursive: true);
            Directory.CreateDirectory(this._TypesDir);
            File.WriteAllLines(Path.Combine(this._TypesDir, "Bar.cs"), new[] { "// <auto-generated by=\"the Blazor I18n Text compiler\" />" });
            File.WriteAllLines(Path.Combine(this._TypesDir, "Bar.cs.bak"), new[] { "// <auto-generated by=\"the Blazor I18n Text compiler\" />" });
            File.WriteAllLines(Path.Combine(this._TypesDir, "Fizz.cs"), new[] { "public class Fizz {}" });

            var srcPath = Path.Combine(Environment.CurrentDirectory, "i18ntext", "Foo.Bar.en.json");
            var srcFiles = new[] { new I18nTextSourceFile(srcPath, Encoding.UTF8) };
            var options = new I18nTextCompilerOptions { OutDirectory = _TextResJsonsDir };
            var compiler = new I18nTextCompiler();
            var suceess = compiler.Compile(srcFiles, options);

            suceess.IsTrue();

            Directory.GetFiles(this._TypesDir)
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name)
                .Is(
                    // "Bar.cs" should be sweeped.
                    "Bar.cs.bak",
                    "Fizz.cs",
                    "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.cs");

            File.ReadLines(Path.Combine(this._TypesDir, "Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.cs"))
                .FirstOrDefault()
                .Is("// <auto-generated by=\"the Blazor I18n Text compiler\" />");
        }

        [Fact(DisplayName = "Compile - sweep I18n Text JSON files")]
        public void Compile_SweepTextJsonFiles_Test()
        {
            if (Directory.Exists(this._TextResJsonsDir)) Directory.Delete(this._TextResJsonsDir, recursive: true);
            Directory.CreateDirectory(this._TextResJsonsDir);
            File.WriteAllLines(Path.Combine(this._TextResJsonsDir, "Bar.json"), new[] { "{\"Key\":\"Value\"}" });

            var srcPath = Path.Combine(Environment.CurrentDirectory, "i18ntext", "Foo.Bar.en.json");
            var srcFiles = new[] { new I18nTextSourceFile(srcPath, Encoding.UTF8) };
            var options = new I18nTextCompilerOptions { OutDirectory = _TextResJsonsDir };
            var compiler = new I18nTextCompiler();
            var suceess = compiler.Compile(srcFiles, options);

            suceess.IsTrue();

            // "Bar.json" should be sweeped.
            Directory.GetFiles(this._TextResJsonsDir)
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name)
                .Is("Toolbelt.Blazor.I18nTextCompileTask.Test.I18nText.Foo.Bar.en.json");
        }
    }
}
