# Blazor I18n Text - .NET 6 Preview Support Information

## Current Status

### [Update] Blazor I18n Text v.11 preview 1 was released! 🎉

**The version 11 Preview 1** of the "Blazor I18n Text" library was released.

- https://www.nuget.org/packages/Toolbelt.Blazor.I18nText/11.0.0-preview.1

This version should work fine also on **.NET 6**.

Check it out!


### About v.10 and v.9 of Blazor I18n Text...

At this time, the "Blazor I18n Text" library **ver.10 and 9** doesn't work well in Blazor WebAssembly applications (includes ASP.NET Core hosted) development stage since .NET 6 Preview 7.

This problem is caused by the architecture changing of static web asset feature in .NET 6 Preview 7.

Fortunately, there are workarounds at this time.

## Workarounds for Blazor I18n Text ver.10 and 9

### for Blazor WebAssembly stand-alone projects:

Try to downgrade the version of the "Microsoft.AspNetCore.Components.WebAssembly.DevServer" package referencing in your project to v.6.0.0-preview.6.21355.2.

```xml
<!-- your .csproj file -->
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="6.0.0-preview.7.21378.6" />
    <!-- 👇 downgrade this to preview 6! -->
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="6.0.0-preview.6.21355.2" PrivateAssets="all" />
    ...
```

This downgrading should be safe yet in the .NET 6 SDK preview 7.

### for Blazor WebAssembly ASP.NET Core hosted projects:

Try to insert some code snippets as below into the Startup class in an ASP.NET Core host server.

```csharp
// your Startup.cs file
public class Startup
{
  ...
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    // 👇 Insert this at the top of the "Configure" method!
    var assembly = Assembly.Load(env.ApplicationName);
    var basePath = (string.IsNullOrEmpty(assembly.Location) ? AppContext.BaseDirectory : Path.GetDirectoryName(assembly.Location));
    env.WebRootFileProvider = new CompositeFileProvider(
      new PhysicalFileProvider(Path.Combine(basePath, "wwwroot")),
      env.WebRootFileProvider);
    ...
```

The code snippets above should be safe and should have no harmful side effects.

## Note

The "Blazor I18n Text" library works well in Blazor Server applications .NET6 Preview 7 too.

And also, even though the Blazor WebAssembly applications will work well if it is published.

## Future Plan

I plan that the future version of the "Blazor I18n Text" library will support .NET 6 well without any workaround like above.

The "Blazor I18n Text" library version 11 preview 1 is one of the important milestones to the goal above.