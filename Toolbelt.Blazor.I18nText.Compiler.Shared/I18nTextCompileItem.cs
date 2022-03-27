using System.Collections.Generic;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText
{
    public readonly struct I18nTextCompileItem
    {
        internal readonly KeyValuePair<string, I18nTextType> Type;
        public readonly string TypeNamespace;
        public readonly string TypeName;
        public readonly string TypeFilePath;

        internal I18nTextCompileItem(KeyValuePair<string, I18nTextType> type, string typeNamespace, string typeName, string typeFilePath)
        {
            this.Type = type;
            this.TypeNamespace = typeNamespace;
            this.TypeName = typeName;
            this.TypeFilePath = typeFilePath;
        }
    }
}
