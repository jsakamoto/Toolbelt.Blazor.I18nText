﻿namespace Toolbelt.Blazor.I18nText.Internals;

internal delegate ValueTask<Dictionary<string, string>?> ReadJsonAsTextMapAsync(string jsonUrl, string hash);
