using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal static class WeakRefCollectionExtensions
    {
        public static void InvokeStateHasChanged(this WeakRefCollection<ComponentBase> components)
        {
            var stateHasChangedMethod = typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            components.ForEach(component =>
            {
                stateHasChangedMethod.Invoke(component, new object[] { });
            });
        }
    }
}
