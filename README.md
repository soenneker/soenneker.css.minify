[![](https://img.shields.io/nuget/v/soenneker.css.minify.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.css.minify/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.css.minify/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.css.minify/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.css.minify.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.css.minify/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.css.minify/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.css.minify/actions/workflows/codeql.yml)

# Soenneker.Css.Minify

A lightweight CSS text and file minifier that removes comments and unnecessary whitespace while preserving token boundaries.

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

`AddCssMinifierAsScoped()` is also available. Both methods register the matching `IFileUtil` lifetime used by file operations. The minifier itself keeps no per-call state, so singleton registration is suitable when its file utility is also singleton.

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

string result = minifier.Minify(css);
// .nav .item001{margin:0 .5rem;color:#001122;width:calc(100% - 1rem)}
```

A `ReadOnlySpan<char>` overload is available when the input already resides in a span. Empty input returns an empty string.

The minifier:

- removes block comments, including `/*! ... */` comments;
- removes redundant whitespace and a final semicolon before `}`;
- preserves quoted strings and escapes;
- preserves required selector, value-list, and `calc()` spacing;
- normalizes numeric forms such as `00.50em` to `.5em` and removes units from zero where supported;
- leaves digits inside identifiers, custom-property names, and hex colors unchanged.

## Minify a file

```csharp
await minifier.MinifyFile(
    inputPath: "wwwroot/css/site.css",
    outputPath: "wwwroot/css/site.min.css",
    cancellationToken);
```

The input file is read completely, minified in memory, and written to the output path through `IFileUtil`. Existing output is replaced according to that utility's write behavior. File errors and cancellation propagate to the caller.

## Scope and validation

This is a purpose-built token minifier, not a full CSS parser. It does not report malformed CSS, resolve imports, add vendor prefixes, rewrite colors, produce source maps, or guarantee every future/proprietary CSS grammar is understood. Run representative browser or CSS parser tests before placing it in a production asset pipeline.

Minification is not sanitization. Do not treat arbitrary CSS as safe merely because it passed through this library.
