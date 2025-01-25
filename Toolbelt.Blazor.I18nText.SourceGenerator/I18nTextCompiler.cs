﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Toolbelt.Blazor.I18nText.Compiler.Shared;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nTextCompiler
    {
        private const string GeneratedMarker = "// <auto-generated by=\"the Blazor I18n Text compiler\" />";

        public static bool Compile(
            IEnumerable<I18nTextSourceFile> srcFiles,
            I18nTextCompilerOptions options,
            Action<I18nTextCompilerOptions, I18nTextCompileItem, IEnumerable<string>> saveCode,
            CancellationToken cancellationToken)
        {
            try
            {
                var i18textSrc = I18nTextSourceFile.Parse(srcFiles, options, cancellationToken);
                OutputTypesFiles(options, i18textSrc, saveCode, cancellationToken);
                return true;
            }
            catch (AggregateException e) when (e.InnerException is I18nTextCompileException compileException)
            {
                options.LogError(compileException);
                return false;
            }
            catch (I18nTextCompileException compileException)
            {
                options.LogError(compileException);
                return false;
            }
        }

        private static void OutputTypesFiles(
            I18nTextCompilerOptions options,
            I18nTextSource i18textSrc,
            Action<I18nTextCompilerOptions, I18nTextCompileItem, IEnumerable<string>> saveCode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!i18textSrc.Types.Any()) return;

            var i18nTextCompilerItems = i18textSrc.Types.Select(type =>
            {
                var typeFullName = options.NameSpace + "." + type.Key;
                var typeNameParts = typeFullName.Split('.');
                var typeNamespace = string.Join(".", typeNameParts.Take(typeNameParts.Length - 1));
                var typeName = typeNameParts.Last();

                return new I18nTextCompileItem(type, typeNamespace, typeName, typeFullName);
            }).ToArray();

            cancellationToken.ThrowIfCancellationRequested();

            // fallback languages is like ["en-US", "en"].
            var fallbackLangs = options.GetFallbackLangCandidates();

            Parallel.ForEach(i18nTextCompilerItems, new ParallelOptions { CancellationToken = cancellationToken }, comilerItem =>
            {
                var langs = comilerItem.Type.Value.Langs;
                var fallbackLang = fallbackLangs.First(lang => langs.ContainsKey(lang));
                var textTable = langs[fallbackLang];

                var hash = GenerateHash(comilerItem.Type.Value);
                cancellationToken.ThrowIfCancellationRequested();

                var typeCode = new List<string>();
                typeCode.Add(GeneratedMarker);
                typeCode.Add($"namespace {comilerItem.TypeNamespace}");
                typeCode.Add("{");
                typeCode.Add($"    public partial class {comilerItem.TypeName} : global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextFallbackLanguage, global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextLateBinding, global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextTableHash");
                typeCode.Add("    {");
                typeCode.Add($"        string global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextTableHash.Hash => \"{hash}\";");
                typeCode.Add("");
                typeCode.Add($"        string global::Toolbelt.Blazor.I18nText.Interfaces.I18nTextFallbackLanguage.FallBackLanguage => \"{options.FallBackLanguage}\";");
                typeCode.Add("");
                typeCode.Add("        public string this[string key] => global::Toolbelt.Blazor.I18nText.I18nTextExtensions.GetFieldValue(this, key);");
                typeCode.Add("");
                var is1stLine = true;
                foreach (var textKey in comilerItem.Type.Value.TextKeys)
                {
                    if (!is1stLine) typeCode.Add("");
                    typeCode.Add($"        /// <summary>\"{EscapeForXMLDocSummary(textTable[textKey])}\"</summary>");
                    typeCode.Add($"        public string {textKey};");
                    is1stLine = false;
                }
                typeCode.Add("    }");
                typeCode.Add("}");

                saveCode?.Invoke(options, comilerItem, typeCode);
            });
        }

        internal static string GenerateHash(I18nTextType i18nText)
        {
            using var hash = SHA256.Create();
            using var stream = new I18nTextTableStream(i18nText);
            var hashBytes = hash.ComputeHash(stream);
            return ToBase36(hashBytes);
        }

        private static string ToBase36(byte[] hash)
        {
            const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";

            var result = new char[10];
            var dividend = BigInteger.Abs(new BigInteger(hash.Take(9).ToArray()));
            for (var i = 0; i < 10; i++)
            {
                dividend = BigInteger.DivRem(dividend, 36, out var remainder);
                result[i] = chars[(int)remainder];
            }

            return new string(result);
        }

        private static string EscapeForXMLDocSummary(string text)
        {
            return SecurityElement.Escape(text).Replace("\r", "").Replace("\n", "<br/>");
        }
    }
}
