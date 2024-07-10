using System.Collections.Concurrent;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Internals
{
    internal class I18nTextSource
    {
        public ConcurrentDictionary<string, I18nTextType> Types { get; } = new ConcurrentDictionary<string, I18nTextType>();
    }
}
