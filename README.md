[![](https://img.shields.io/nuget/v/soenneker.css.minify.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.css.minify/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.css.minify/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.css.minify/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.css.minify.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.css.minify/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.css.minify/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.css.minify/actions/workflows/codeql.yml)

# Soenneker.Css.Minify

A CSS text and file minifier powered by Soenneker.Esbuild.Util.

## Installation

```bash
dotnet add package Soenneker.Css.Minify
```

## Registration

```csharp
using Soenneker.Css.Minify.Abstract;
using Soenneker.Css.Minify.Registrars;

services.AddCssMinifierAsSingleton();

ICssMinifier minifier = serviceProvider.GetRequiredService<ICssMinifier>();
```

`AddCssMinifierAsScoped()` is also available. Both methods register matching lifetimes for `IEsbuildUtil` and `IFileUtil`.

## Minify CSS text

```csharp
const string css = """
    /* navigation */
    .nav .item001 {
        margin: 0px  0.50rem;
        color: #001122;
        width: calc(100% - 1rem);
    }
    """;

string result = await minifier.Minify(css, cancellationToken);
// Returns the CSS minified by esbuild.
```

Text minification is asynchronous and accepts a cancellation token. Null, empty, and whitespace input returns an empty string. Span callers should convert their input to a string before calling.

Nonempty input uses esbuild's CSS loader with minification enabled, without bundling or resolving imports. Output follows esbuild defaults, including legal-comment preservation and a trailing newline. Browser targets are not configured, so output assumes modern CSS support.

## Minify a file

```csharp
await minifier.MinifyFile(
    inputPath: "wwwroot/css/site.css",
    outputPath: "wwwroot/css/site.min.css",
    cancellationToken);
```

The input file is read completely, minified in memory, and written to the output path through `IFileUtil`. Existing output is replaced according to that utility's write behavior. File errors and cancellation propagate to the caller.

## Scope and validation

The esbuild utility manages its Node/npm installation and checks npm for the latest esbuild release on each operation. Nonempty minification therefore requires network access, a writable installation directory, and permission to launch processes. This is intended for asset/build pipelines; cache generated output rather than minifying on every web request.

Esbuild failures and cancellation propagate to the caller. Minification can rewrite CSS syntax and colors, and output may change as esbuild updates.

Minification is not sanitization. Do not treat arbitrary CSS as safe merely because it passed through this library.
