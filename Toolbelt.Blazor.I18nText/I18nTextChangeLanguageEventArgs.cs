using System;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nTextChangeLanguageEventArgs : EventArgs
    {
        public string LanguageCode { get; }

        public I18nTextChangeLanguageEventArgs(string langCode)
        {
            LanguageCode = langCode;
        }
    }
}
