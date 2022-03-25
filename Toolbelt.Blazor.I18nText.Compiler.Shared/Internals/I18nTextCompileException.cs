using System;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class I18nTextCompileException : Exception
    {
        public int Code { get; }

        public I18nTextCompileException(int code, string message) : base(message)
        {
            this.Code = code;
        }
    }
}
