namespace Toolbelt.Blazor.I18nText.Interfaces;

internal interface ITextMapReader
{
    ValueTask<Dictionary<string, string>?> ReadAsync(string jsonUrl, string textTableHash);
}
