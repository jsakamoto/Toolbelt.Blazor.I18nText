using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Toolbelt.Blazor.I18nText.Internals;

/// <summary>
/// Extensions for <see cref="WeakRefCollection{T}"/>.
/// </summary>
internal static class WeakRefCollectionExtensions
{
    /// <summary>
    /// Invokes the <see cref="ComponentBase.StateHasChanged"/> method of all components in the collection.
    /// </summary>
    /// <param name="components">A weak reference collection of components.</param>
    public static void InvokeStateHasChanged(this WeakRefCollection<ComponentBase> components)
    {
        components.ForEach(component =>
        {
            var handleEvent = component as IHandleEvent;
            handleEvent?.HandleEventAsync(EventCallbackWorkItem.Empty, null);
        });
    }

    /// <summary>
    /// Same as <see cref="InvokeStateHasChanged(WeakRefCollection{ComponentBase})"/> mthod, but thread-safe.
    /// </summary>
    /// <param name="components">A weak reference collection of components.</param>
    public static void InvokeStateHasChangedThreadSafe(this WeakRefCollection<ComponentBase> components)
    {
        var invokeAsyncMethod = typeof(ComponentBase).GetMethod("InvokeAsync", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(Func<Task>) });
        if (invokeAsyncMethod is null) throw new MissingMethodException("InvokeAsync(Func<Task>) method is not found in the ComponentBase class.");

        components.ForEach(component =>
        {
            var handleEvent = component as IHandleEvent;
            if (handleEvent is null) return;
            Func<Task> f = () => handleEvent.HandleEventAsync(EventCallbackWorkItem.Empty, null);
            invokeAsyncMethod.Invoke(component, new object[] { f });
        });
    }
}
