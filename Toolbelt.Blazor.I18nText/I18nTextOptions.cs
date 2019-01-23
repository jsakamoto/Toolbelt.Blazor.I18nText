using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbelt.Blazor.I18nText
{
    public delegate string GetInitialLanguage();

    public delegate Task PersistCurrentLanguageAsync(string langCode);

    public class I18nTextOptions
    {
        public GetInitialLanguage GetInitialLanguage { get; set; }
        public PersistCurrentLanguageAsync PersistCurrentLanguageAsync { get; set; }
    }
}
