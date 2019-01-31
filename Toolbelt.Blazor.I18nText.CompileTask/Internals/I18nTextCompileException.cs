using System;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class I18nTextCompileException : Exception
    {
        public I18nTextCompileException(string message) : base(message)
        {
        }
    }
}
