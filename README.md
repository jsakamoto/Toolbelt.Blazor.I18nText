# Blazor Internationalization(I18n) Text [![NuGet Package](https://j.mp/2nZUe7M)](https://j.mp/2nbtArW)

## Summary

This package allows you to localize texts in your Blazor app.

![movie.1](https://j.mp/2kwwHth)

### Features

- It works with both server-side Blazor Server app and client-side Blazor WebAssembly app.
- On the client-side Blazor WebAssembly app, it works without Server-Side runtime (requires only static contents HTTP server).
- You can develop with only plain text editor - No require .resx
- Static Typing - IntelliSense, Code Hint...
- It also works well on Blazor components libraries, and you can package and redistribute the library that is localized with "Blazor I18nText" as a NuGet package.

### Live Demo

- [https://jsakamoto.github.io/Toolbelt.Blazor.I18nText/](https://j.mp/2lFlwyp)

### Supported Blazor versions

"Blazor I18n Text" ver.7.x supports following Blazor versions:

- server-side Blazor Server App **v.3.0.0**
- client-side Blazor WebAssembly App **v.3.0.0 preview 9**

## Quick Start

### Step.1 - Add Package

Add `Toolbelt.Blazor.I18nText` NuGet package to your Blazor app project.

If you are using dotnet CLI, you can do it by command line as bellow.

```
$ dotnet add package Toolbelt.Blazor.I18nText
```

You can also do it in Package Manager Console of Visual Studio, if you are using Visual Studio on Windows OS.

```
PM> Install-Package Toolbelt.Blazor.I18nText
```

### Step.2 - Create localized text source files as JSON or CSV

Add localized text source files for each language in an `i18ntext` folder under the project folder.

The localized text source files must be simple key-value only JSON file like a bellow example,

```json
{
  "Key1": "Localized text 1",
  "Key2": "Localized text 2",
  ...
}
```

or, 2 columns only CSV file without header row like a bellow example.

```
Key1,Localized text 1
Key2,Localized text 2
```

**NOTICE** - The encoding of the CSV file must be **UTF-8**.


And, the naming rule of localized text source files must be bellow.

```
<Text Table Name>.<Language Code>.{json|csv}
```

![fig.1](https://j.mp/2lDwogA)

### Step.3 - Build the project whenever localized text source files are created or updated.

After creating or updating those localized text source files, **you have to build your Blazor app project.**

After building the project, **"Typed Text Table class" C# files** will be generated in the `i18ntext/@types` folder, by the building process.

And also, **"Localized Text Resource JSON" files** will be generated in the output folder, too.

![fig.2](https://j.mp/2ktuz5m)

**NOTE** - If you want to do this automatically whenever those localized text source files (.json or .csv) are changed, you can use `dotnet watch` command with the following arguments.

```shell
$ dotnet watch msbuild -t:CompileI18nText
```

After entry this dotnet CLI command, dotnet CLI stay in execution state and watch the changing of localized text source files. If it detects the changing of localized text source files, then the dotnet CLI re-compile localized text source files into **"Typed Text Table class"** files and **"Localized Text Resource JSON" files**.

![fig.2-2](https://j.mp/2lYJC7z)

### Step.4 - Configure your app to use I18nText service

Edit the "Startup" class in your Blazor app to register "I18nText" service, like this.

![fig.3](https://j.mp/2k0lv7R)

### Step.5 - Get the "Text Table" object in your Blazor component

Open your Blazor component file (.razor) in your editor, and do this:

1. Inject `Toolbelt.Blazor.I18nText.I18nText` service into the component.

```csharp
@inject Toolbelt.Blazor.I18nText.I18nText I18nText
```

2. Add a filed of the Text Table class generated from localized text source files, and assign the default instance.

```csharp
@code {
  I18nText.MyText MyText = new I18nText.MyText();
```

**NOTE** - The namespace of the Text Table class is `<default namespace of your Blazor project>` + `"I18nText"`.

3. Override `OnInitAsync()` method of the Blazor component, and assign a Text Table object that's a return value of `GetTextTableAsync<T>()` method of `I18nText` service instance to the Text Table field.

```csharp
protected override async Task OnInitializedAsync()
{
  MyText = await I18nText.GetTextTableAsync<I18nText.MyText>(this);
```

![fig.4](https://j.mp/2ktiyNr)

### Step.6 - Use the Text Table

After doing these steps, you can reference a field of the Text Table object to get localized text.

If you are using Visual Studio in Windows OS and Blazor extensions is installed in that Visual Studio, you can get "IntelliSense" and "Document comment" support.

![movie.2](https://j.mp/2kjWG7i)

**_Note:_** Text Table object allows you to get localized text by key string dynamically, with indexer syntax, like this.

```html
<h1>@MyText["HelloWorld"]</h1>
```

This way is sometimes called "late binding".

This feature is very useful in some cases.  
However, if you make some mistakes that typo of key string, these mistakes will not be found at compile time. 
In this case, it will just return the key string as is without any runtime exceptions.

### Step.7 - Run it!

Build and run your Blazor app.

The I18nText service detects the language settings of the Web browser, and reads the localized text resource JSON which is most suitable for the language detected.

![fig.5](https://j.mp/2lAfCia)

### More information for in case of server-side Blazor server app

I recommend enabling "Request Localization" middleware on the server-side Blazor server app, by like the following code.

```csharp
...
public class Startup
{
  ...
  public void ConfigureServices(IServiceCollection services)
  {
    services.Configure<RequestLocalizationOptions>(options => {
      var supportedCultures = new[] { "en", "ja" };
      options.DefaultRequestCulture = new RequestCulture("en");
      options.AddSupportedCultures(supportedCultures);
      options.AddSupportedUICultures(supportedCultures);
    });
    ...

  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    app.UseRequestLocalization();
    ...
```

This code makes the result of server-side pre-rendering to be suitable for "Accept-Language" header value in a request from clients.

## Limitations

The following features are not supported in this version of `I18Text` library.

- Integration with ASP.NET Core localization (`IStringLocalizer<T>` support)
- Localize validation message
- Plural form support
- Text formatting by place holder. (You can use `System.String.Format(...)` instead.)
- Integration with `System.Globalization.Culture.CurrentUICulture`.

The following features will not be supported forever, because these features are not the scope of this library, I think.

- Formatting of date, time, currency. (These features will be provided by `System.Globalization.Culture`.)

## Configuration

### Fallback language

Fallback language is determined at compile time.

The default fallback language is `en`.

If you want to change the fallback language, edit your project file (.csproj) to add `<I18nTextFallBackLanguage>` MSBuild property with the language code what you want.

![fig.6](https://j.mp/2k0kTz5)

### The namespace of the Text Table class

If you want to change the namespace of the Text Table classes that will be generated by the building process, edit your project file (.csproj) to add `<I18nTextNamespace>` MSBuild property with the namespace what you want.

![fig.7](https://j.mp/2ktvqmH)

## API Reference

Please see also ["API Reference"](https://j.mp/2kjVssG) on GitHub.

## Release Note

Release note is [here.](http://j.mp/2oSB9UP)

## License

[Mozilla Public License Version 2.0](https://j.mp/2lxriCv)
