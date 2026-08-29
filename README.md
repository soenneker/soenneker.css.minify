[![](https://img.shields.io/nuget/v/soenneker.css.minify.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.css.minify/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.css.minify/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.css.minify/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.css.minify.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.css.minify/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.css.minify/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.css.minify/actions/workflows/codeql.yml)

# Soenneker.Css.Minify

A utility that minifies stylesheets.

## Install

```bash
dotnet add package Soenneker.Css.Minify
```

## Quick start

```csharp
using Soenneker.Css.Minify.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddCssMinifierAsScoped();
```

Adds as a scoped service.

## What you get

- `ICssMinifier` — A utility that minifies stylesheets.
- `CssMinifierRegistrar` — A utility that minifies stylesheets.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `ICssMinifier.Minify(css)` | Minifies the supplied CSS text. | The minified CSS text. |
| `CssMinifierRegistrar.AddCssMinifierAsScoped(services)` | Adds as a scoped service. | The same service collection, so additional registrations can be chained. |
| `CssMinifierRegistrar.AddCssMinifierAsSingleton(services)` | Adds as a singleton service. | The same service collection, so additional registrations can be chained. |
