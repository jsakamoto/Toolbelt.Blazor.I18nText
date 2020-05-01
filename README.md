# Blazor Internationalization(I18n) Text [![NuGet Package](https://j.mp/2nZUe7M)](https://j.mp/2nbtArW)

## Summary

This package is an another way to localize text in your Blazor Web App!

![movie.1](https://j.mp/2kwwHth)

- [Live Demo is here!](https://j.mp/2lFlwyp)

### Features

- It works on both Blazor Server and Blazor WebAssembly.
- In Blazor WebAssembly, it works without the Server-Side process (requires only static contents HTTP server).
- You can develop with an only plain text editor - No require .resx
- Static Typing - IntelliSense, Code Hint...
- It also works well on Blazor components libraries. You can package the library that's localized with "Blazor I18nText" as a NuGet package.

### Notice

Now, Blazor WebAssembly has been started the localization support officially since v.3.2 preview 4. It is based on .NET Standard `IStringLocalizer` and satellite assemblies with `.resx`.

However, I will continue to develop and maintain this package, because this package still has some advantages against .NET standard way.

![pros/cons](https://j.mp/2Yd2Rv4)

### Supported versions

"Blazor I18n Text" ver.9.x supports following Blazor versions:

- Blazor Server App **v.3.0.0 ~ 3.1.0 GA**
- Blazor WebAssembly App **v.3.2.0 Preview 2~5, and Release Candidate 1**

## Quick Start

### Step.1 - Add Package

Add `Toolbelt.Blazor.I18nText` NuGet package to your Blazor app project, like this.

```
$ dotnet add package Toolbelt.Blazor.I18nText
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

After entry this dotnet CLI command, the command stay in execution and watch the changing of localized text source files. If it detects the changing of localized text source files, then the dotnet CLI re-compile localized text source files into **"Typed Text Table class"** and **"Localized Text Resource JSON"** files.

![fig.2-2](https://j.mp/2lYJC7z)

### Step.4 - Configure your app to use I18nText service

Edit the "Startup" class to register "I18nText" service, like this.

```csharp
// in your Startup.cs
using Toolbelt.Blazor.Extensions.DependencyInjection; // <- Add this, and...
...
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddI18nText(); // <- Add this.
    ...
```

Your project is Blazor WebAssembly v.3.2+, you should edit "Program" class to do this.

```csharp
// in your Program.cs
using Toolbelt.Blazor.Extensions.DependencyInjection; // <- Add this, and...
...
public static async Task Main(string[] args)
{
  var builder = WebAssemblyHostBuilder.CreateDefault(args);
  ...
  builder.Services.AddI18nText(); // <- Add this.
  ...
```

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

3. Override `OnInitiallizedAsync()` method of the Blazor component, and assign a Text Table object that's a return value of `GetTextTableAsync<T>()` method of `I18nText` service instance to the Text Table field.

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

If you make some mistakes that typo of key string, it will just return the key string as is without any runtime exceptions.

### Step.7 - Run it!

Build and run your Blazor app.

The I18nText service detects the language settings of the Web browser, and reads the localized text resource JSON which is most suitable for the language detected.

![fig.5](https://j.mp/2lAfCia)

### More information for in case of Blazor server app

I recommend enabling "Request Localization" middleware on the Blazor server app, by like the following code.

```csharp
// in the Startup class
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
- Text formatting by place holder.
- Integration with `System.Globalization.Culture.CurrentUICulture`.

The following features will not be supported forever, because these features are not the scope of this library, I think.

- Formatting of date, time, currency. (These features will be provided by `System.Globalization.Culture`.)

## Configuration

- [Fallback language](https://github.com/jsakamoto/Toolbelt.Blazor.I18nText/blob/master/README-CONFIGURATION.md#fallback-language)
- [The namespace of the Text Table class](https://github.com/jsakamoto/Toolbelt.Blazor.I18nText/blob/master/README-CONFIGURATION.md#the-namespace-of-the-text-table-class)

## API Reference

Please see also ["API Reference"](https://j.mp/2kjVssG) on GitHub.

## Release Notes

Release notes is [here.](https://j.mp/3aWxQhQ)

## License

[Mozilla Public License Version 2.0](https://j.mp/2lxriCv)
