using System;
using Toolbelt.Blazor.I18nText.SourceGenerator.Inetrnals;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class I18nTextCompileException : Exception
    {
        public DiagnosticCode Code { get; }

        public string FilePath { get; }

        public int LinePos { get; }

        public I18nTextCompileException(DiagnosticCode code, string message, string filePath = null, int linePos = 0) : base(message)
        {
            this.Code = code;
            this.FilePath = filePath;
            this.LinePos = linePos;
        }
    }
}
