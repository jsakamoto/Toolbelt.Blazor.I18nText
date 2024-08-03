using Microsoft.AspNetCore.Components;

namespace Toolbelt.Blazor.I18nText.Internals;

internal static class WeakRefCollectionExtensions
{
    public static void InvokeStateHasChanged(this WeakRefCollection<ComponentBase> components)
    {
        components.ForEach(component =>
        {
            var handleEvent = component as IHandleEvent;
            handleEvent?.HandleEventAsync(EventCallbackWorkItem.Empty, null);
        });
    }
}
