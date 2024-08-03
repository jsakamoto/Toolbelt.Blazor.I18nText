using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Toolbelt.Blazor.I18nText;

public delegate ValueTask<string> GetInitialLanguage(IServiceProvider serviceProvider, I18nTextOptions options);

public delegate ValueTask PersistCurrentLanguageAsync(IServiceProvider serviceProvider, string langCode, I18nTextOptions options);

public delegate void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client);

public class I18nTextOptions
{
    internal const string DefaultHttpClientName = "Toolbelt.Blazor.I18nText.HttpClient";

    public GetInitialLanguage? GetInitialLanguageAsync;

    public PersistCurrentLanguageAsync? PersistCurrentLanguageAsync;

    [Obsolete("Use the \"I18nTextOptions.PersistenceLevel\" instead."), EditorBrowsable(EditorBrowsableState.Never)]
    public PersistanceLevel PersistanceLevel = PersistanceLevel.Session;

    /// <summary>
    /// Gets or sets the persistence level of the current language. The default value is "Session".<br/>
    /// This property represents which store place used to persist the current language.<br/>
    /// When you use the "I18nText" service on Blazor SSR, we recommend using "Cookie" or "PersistentCookie" to persist the current language.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public PersistanceLevel PersistenceLevel { get => this.PersistanceLevel; set => this.PersistanceLevel = value; }
#pragma warning restore CS0618 // Type or member is obsolete

    public string? HttpClientName = DefaultHttpClientName;

    [Obsolete("The \"I18nTextOptions.IsWasm()\" is no longer used from anywhere."), EditorBrowsable(EditorBrowsableState.Never)]
    public Func<bool>? IsWasm;

    /// <summary>
    /// Gets or sets the function to configure the named HttpClient used by the I18nText service.
    /// </summary>
    public ConfigureHttpClient? ConfigureHttpClient = null;

    /// <summary>
    /// Gets of sets the log level of the named HttpClient used by the I18nText service.<br/>
    /// The default value is <see cref="LogLevel.Warning"/>.
    /// </summary>
    public LogLevel HttpClientLogLevel = LogLevel.Warning;

    /// <summary>
    /// The name of the cookie to store the current language when the <see cref="PersistenceLevel"/> is "Cookie" or "PersistentCookie".<br/>
    /// The default value is null, which means that the cookie name is ".AspNetCore.Culture".
    /// </summary>
    public string CookieName { get; set; } = ".AspNetCore.Culture";

    /// <summary>
    /// The storage key to store the current language when the <see cref="PersistenceLevel"/> is "Session" or "SessionAndLocal".<br/>
    /// The default value is null, which means that the key is "Toolbelt.Blazor.I18nText.CurrentLanguage".
    /// </summary>
    public string StorageKey { get; set; } = "Toolbelt.Blazor.I18nText.CurrentLanguage";
}
