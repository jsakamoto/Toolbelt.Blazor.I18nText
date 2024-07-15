using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Toolbelt.Blazor.I18nText.Interfaces;

namespace Toolbelt.Blazor.I18nText.Internals;

internal class TextMapReaderForWasm : ITextMapReader, IDisposable
{
    private readonly HelperScript _helperScript;

    private readonly HttpClient _httpClient;

    public TextMapReaderForWasm(I18nTextOptions options, HelperScript helperScript, IHttpClientFactory httpClientFactory)
    {
        this._helperScript = helperScript;
        this._httpClient = httpClientFactory.CreateClient(options.HttpClientName ?? "Toolbelt.Blazor.I18nText.HttpClient");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Dictionary<string, string>)), SuppressMessage("Trimming", "IL2026")]
    public async ValueTask<Dictionary<string, string>?> ReadAsync(string jsonUrl, string textTableHash)
    {
        var isOnline = await this._helperScript.IsOnlineAsync();
        if (isOnline && !string.IsNullOrEmpty(textTableHash)) jsonUrl += "?hash=" + textTableHash;

        var httpRes = await this._httpClient.GetAsync(jsonUrl);
        if (httpRes.StatusCode == HttpStatusCode.NotFound) return null;
        var contentBytes = await httpRes.Content.ReadAsByteArrayAsync();
        return JsonSerializer.Deserialize<Dictionary<string, string>?>(contentBytes);
    }

    public void Dispose()
    {
        this._httpClient.Dispose();
    }
}
