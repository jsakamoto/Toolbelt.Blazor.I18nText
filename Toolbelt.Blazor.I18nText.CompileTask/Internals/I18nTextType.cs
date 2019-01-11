using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class I18nTextType
    {
        public List<string> TextKeys { get; set; }

        public ConcurrentDictionary<string, I18nTextTable> Langs { get; } = new ConcurrentDictionary<string, I18nTextTable>();
    }
}
