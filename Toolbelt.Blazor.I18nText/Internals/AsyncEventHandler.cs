namespace Toolbelt.Blazor.I18nText.Internals;

internal delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e) where TEventArgs : EventArgs;
