# Appendix: On .NET 5 or earlier / Blazor I18n Text ver.11 or earlier

## Summary

The source generator for Blazor I18n Text is enabled on .NET 6 or later and Blazor I18n Text ver.12 only.

This document explains compiling localized text source files behavior on .NET 5 or earlier platforms or ver.11 or earlier Blazor I18n Text.

### Compiling localized text source files behavior

After creating or updating those localized text source files, **you have to build your Blazor app project.**

After building the project, **"Typed Text Table class" C# files** will be generated in the `i18ntext/@types` folder, by the building process.

And also, **"Localized Text Resource JSON" files** will be generated in the output folder, too.

![fig.2](https://raw.githubusercontent.com/jsakamoto/assets/m/i18n/fig2.png)

## dotnet watch

When you are developing on dotnet CLI and want to do this automatically whenever those localized text source files (.json or .csv) are changed, you can use the `dotnet watch` command with the following arguments.

```shell
$ dotnet watch msbuild -t:CompileI18nText
```

After entering that dotnet CLI command, the command window will stay in execution mode and watch localized text source files changing.

When that dotnet CLI detects localized text source files changing, the dotnet CLI will recompile localized text source files into **"Typed Text Table class"** and **"Localized Text Resource JSON"** files.

![fig.2-2](https://raw.githubusercontent.com/jsakamoto/assets/m/i18n/fig2b.png)

