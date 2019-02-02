using System.Threading.Tasks;

namespace Toolbelt.Blazor.I18nText
{
    public delegate Task<string> GetInitialLanguage(I18nTextOptions options);

    public delegate Task PersistCurrentLanguageAsync(string langCode, I18nTextOptions options);

    public class I18nTextOptions
    {
        public GetInitialLanguage GetInitialLanguageAsync { get; set; }

        public PersistCurrentLanguageAsync PersistCurrentLanguageAsync { get; set; }

        public PersistanceLevel PersistanceLevel { get; set; } = PersistanceLevel.SessionAndLocal;
    }
}
