using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Toolbelt.Blazor.I18nText.Compiler.Shared.Internals
{
    internal class I18nTextTable : ConcurrentDictionary<string, string>
    {
        public I18nTextTable(IEnumerable<KeyValuePair<string, string>> collection) : base(collection)
        {
        }
    }
}
