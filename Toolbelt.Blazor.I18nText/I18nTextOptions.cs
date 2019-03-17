using System;
using System.Threading.Tasks;

namespace Toolbelt.Blazor.I18nText
{
    public delegate Task<string> GetInitialLanguage(IServiceProvider serviceProvider, I18nTextOptions options);

    public delegate Task PersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options);

    public class I18nTextOptions
    {
        public GetInitialLanguage GetInitialLanguageAsync;

        public PersistCurrentLanguageAsync PersistCurrentLanguageAsync;

        public PersistanceLevel PersistanceLevel = PersistanceLevel.Session;
    }
}
