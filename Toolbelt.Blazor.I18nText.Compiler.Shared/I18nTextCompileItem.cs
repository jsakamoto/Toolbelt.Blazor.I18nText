using System.Collections.Generic;
using Toolbelt.Blazor.I18nText.Compiler.Shared.Internals;

namespace Toolbelt.Blazor.I18nText.Compiler.Shared
{
    public readonly struct I18nTextCompileItem
    {
        internal KeyValuePair<string, I18nTextType> Type { get; }

        public string TypeNamespace { get; }

        public string TypeName { get; }

        /// <summary>
        /// The file name of the generated type. (e.g. "Name.Space.TypeName")
        /// </summary>
        public string TypeFileName { get; }

        internal I18nTextCompileItem(KeyValuePair<string, I18nTextType> type, string typeNamespace, string typeName, string typeFileName)
        {
            this.Type = type;
            this.TypeNamespace = typeNamespace;
            this.TypeName = typeName;
            this.TypeFileName = typeFileName;
        }
    }
}
