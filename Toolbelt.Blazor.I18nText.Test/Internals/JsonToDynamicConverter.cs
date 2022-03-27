/*
MIT License

Copyright (c) 2020 m-ishizaki

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
*/

// https://github.com/m-ishizaki/JsonDocumentToDynamic

using System.Dynamic;
using System.Text.Json;

namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal static class JsonToDynamicConverter
{
    public static ExpandoObject ToExpandoObject(this JsonElement element) => toExpandoObject(element);

    public static ExpandoObject ToExpandoObject(this JsonDocument document) => toExpandoObject(document.RootElement);

    public static ExpandoObject Parse(string json, JsonDocumentOptions options = default)
    {
        using var document = JsonDocument.Parse(json, options);
        return toExpandoObject(document.RootElement);
    }

    private static object? propertyValue(JsonElement elm) =>
        elm.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Number => elm.GetDecimal(),
            JsonValueKind.String => elm.GetString(),
            JsonValueKind.False => false,
            JsonValueKind.True => true,
            JsonValueKind.Array => elm.EnumerateArray().Select(m => propertyValue(m)).ToArray(),
            _ => toExpandoObject(elm),
        };

    private static ExpandoObject toExpandoObject(JsonElement elm) =>
        elm.EnumerateObject()
        .Aggregate(
            new ExpandoObject(),
            (exo, prop) => { ((IDictionary<string, object?>)exo).Add(prop.Name, propertyValue(prop.Value)); return exo; });
}