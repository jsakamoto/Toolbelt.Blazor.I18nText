# API Reference

## I18nText service class

### namespace

Toolbelt.Blazor.I18nText


### GetCurrentLanguageAsync method

#### Syntax

```csharp
public Task<string> GetCurrentLanguageAsync();
```

#### Description

This method returns the language code of the current selecting in the `I18nText` service instance.

That language code is what detected from the Web browser settings, or specified by an argument of last `SetCurrentLanguageAsync()` method call.

`GetTextTableAsync<T>(...)` method uses the language code that this method returns to determine which language resource file should load.

### SetCurrentLanguageAsync method

#### Syntax

```csharp
public Task<string> SetCurrentLanguageAsync(string langCode);
```

#### Description

This method changes the language code of current selecting in the I18nText service instance.

In the default configuration, the language code that passed to the argument of this method will stored in session storage of the web browser. 

The language code that was stored by this method will be read at launching the Blazor app, and used to initialize the `I18nText` service instance.

If you want to change this storing behavior, you can configure it at service registration (see also: `AddI18nText<T>(...)` extension method.).

### GetTextTableAsync method

#### Syntax

```csharp
public Task<T> GetTextTableAsync<T>(ComponentBase component);
```

#### Description


This method returns the "Text Table" object specified with type argument `T`.
The type `T` is the class auto generated from localized text source file in the building process.

The fields of "Text Table" object are initialized by Localized Resource Text JSON file which is most suitable for the language code returned by the `GetCurrentLanguageAsync()` method.

When the `SetCurrentLanguageAsync(...)` method is invoked and current language of the `I18nText` service instance is changed, `StateHasCahnged()` method of the component that is specified to the 1st argument of this method will be invoked automatically.  
By this effect, the result of rendering Blazor component will be refreshed with the after changed language's localized text.

## I18nTextDependencyInjection class

### namespace

Toolbelt.Blazor.Extensions.DependencyInjection

### AddI18nText extension method

#### Syntax

```csharp
public static IServiceCollection AddI18nText<TStartup>(
  this IServiceCollection services,
  Action<I18nTextOptions> configure = null);
```

### Description

This extension method registers `I18nText` service into .NET Core DI (Dependency Injection) system.

You can customize the behavior of `I18nText` service in `configure` callback function.

## I18nTextOptions class

### namespace

Toolbelt.Blazor.I18nText

### GetInitialLanguageAsync field

#### Syntax

```csharp
public GetInitialLanguage GetInitialLanguageAsync;
```

#### Description

This field defines the behavior of how to get a initial language code when the Blazor app loaded first.

The type of this field is a delegate that has following syntax.

```csharp
public delegate Task<string> GetInitialLanguage(I18nTextOptions options);
```

By the default configuration, this field points to the static method that implements default behavior.  
The default implements will return the language code from the web browser's local or session storage that would be stored by `SetCurrentLanguageAsync(...)` method, or web browser's language settings if the storage is empty.

### PersistCurrentLanguageAsync field

### Syntax

```csharp
public PersistCurrentLanguageAsync PersistCurrentLanguageAsync;
```

#### Description

This field defines the behavior of how to persist the current language selecting in the `I18nText` service instance, when the `SetCurrentLanguageAsync(...)` method is invoked.

The type of this filed is a delegate that has following syntax.

```csharp
public delegate Task PersistCurrentLanguageAsync(
  string langCode,
  I18nTextOptions options);
```

By the default configuration, this field points to the static method that will or will not store the language code that is specified for the argument of the delegate.  
The behavior of the default implements rely on what value is specified in the`PersistanceLevel` filed of the `option` argument.

### PersistanceLevel field

#### Syntax

```csharp
public PersistanceLevel PersistanceLevel;
```

#### Description

This enum field allows you to control which storage is used for storing the current language selecting when the `SetCurrentLanguageAsync(..)` method is invoked.

The type of this field is a enum type that has following values, and this field has one of the these enum values.

```csharp
public enum PersistanceLevel
{
  None,
  Session,
  SessionAndLocal
}
```

These enumerated values ​​have the following meanings.

- **None** ... the language code will not persist anywhere.
- **Session** ... the language code will persist into web borwser's session storage.
- **SessionAndLocal** ... the language code will persist into web borwser's session storage and local storage.

This field value is used by the static method that is the default value of `PersistCurrentLanguageAsync` field.