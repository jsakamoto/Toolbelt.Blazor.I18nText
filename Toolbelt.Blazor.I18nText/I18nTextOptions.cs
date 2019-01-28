using System.Threading.Tasks;

namespace Toolbelt.Blazor.I18nText
{
    public delegate string GetInitialLanguage(I18nTextOptions options);

    public delegate Task PersistCurrentLanguageAsync(string langCode, I18nTextOptions options);

    public class I18nTextOptions
    {
        public GetInitialLanguage GetInitialLanguage { get; set; }

        public PersistCurrentLanguageAsync PersistCurrentLanguageAsync { get; set; }

        public PersistanceLevel PersistanceLevel { get; set; } = PersistanceLevel.SessionAndLocal;
    }
}
