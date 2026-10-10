using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Css.Minify.Abstract;

/// <summary>
/// Minifies stylesheets using esbuild. Nonempty input requires the Node/npm runtime and network access
/// to resolve the latest esbuild release. Output follows esbuild's formatting and legal-comment defaults.
/// </summary>
public interface ICssMinifier
{
    /// <summary>Minifies CSS without bundling or resolving imports. Null, empty, or whitespace input returns an empty string.</summary>
    /// <param name="css">The CSS text to minify.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The minified CSS text.</returns>
    ValueTask<string> Minify(string? css, CancellationToken cancellationToken = default);

    /// <summary>Reads a stylesheet, minifies it without bundling, and writes the result to the output path.</summary>
    /// <param name="inputPath">Path of the stylesheet to read.</param>
    /// <param name="outputPath">Path to write the minified stylesheet to.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the output has been written.</returns>
    ValueTask MinifyFile(string inputPath, string outputPath, CancellationToken cancellationToken = default);
}
