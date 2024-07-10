using System;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Internals
{
    internal class I18nTextCompileException : Exception
    {
        public DiagnosticCode Code { get; }

        public string FilePath { get; }

        public int LineNumber { get; }

        public int LinePos { get; }

        public I18nTextCompileException(DiagnosticCode code, string message, string filePath = null, int lineNumber = 0, int linePos = 0) : base(message)
        {
            this.Code = code;
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
            this.LinePos = linePos;
        }
    }
}
