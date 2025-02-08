// The assembly attribute "MetadataUpdateHandler(typeof(HotReloadHandler))" will apply
// by the "MetadataUpdateHandlerAttribute.cs" that is bundled with the NuGet package.

using System.ComponentModel;

namespace Toolbelt.Blazor.I18nText.Internals;

internal class HotReloadEventArgs : EventArgs
{
    public Type[]? UpdatedTypes { get; }

    public HotReloadEventArgs(Type[]? updatedTypes)
    {
        this.UpdatedTypes = updatedTypes;
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class HotReloadHandler
{
    internal static event EventHandler<HotReloadEventArgs>? OnClearCache;

    internal static event EventHandler<HotReloadEventArgs>? OnUpdateApplication;

    public static void ClearCache(Type[]? updatedTypes)
    {
        OnClearCache?.Invoke(null, new HotReloadEventArgs(updatedTypes));
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        OnUpdateApplication?.Invoke(null, new HotReloadEventArgs(updatedTypes));
    }
}
