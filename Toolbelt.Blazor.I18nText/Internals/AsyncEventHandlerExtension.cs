namespace Toolbelt.Blazor.I18nText.Internals;

internal static class AsyncEventHandlerExtension
{
    public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs>? asyncEventHandler, object? sender, TEventArgs args) where TEventArgs : EventArgs
    {
        if (asyncEventHandler == null) return;

        var asyncHandlerTasks = asyncEventHandler.GetInvocationList()
            .Cast<AsyncEventHandler<TEventArgs>>()
            .Select(handler => handler.Invoke(sender, args))
            .ToArray();
        await Task.WhenAll(asyncHandlerTasks);
    }
}
