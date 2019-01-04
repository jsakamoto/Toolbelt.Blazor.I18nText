using System;
using System.Threading.Tasks;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class TextTable
    {
        public Type TableType;

        public object Table;

        public Func<object, Task> RefreshTableAsync;
    }
}
