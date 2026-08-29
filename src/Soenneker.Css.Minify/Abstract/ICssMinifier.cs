using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Css.Minify.Abstract;

/// <summary>
/// A utility that minifies stylesheets
/// </summary>
public interface ICssMinifier
{
    /// <summary>
    /// Minifies the supplied CSS text.
    /// </summary>
    /// <param name="css">Css for the minify operation.</param>
    /// <returns>The minified CSS text.</returns>
    string Minify(string css);
    /// <summary>
    /// Minifies the supplied CSS text.
    /// </summary>
    /// <param name="css">Css for the minify operation.</param>
    /// <returns>The minified CSS text.</returns>
    string Minify(ReadOnlySpan<char> css);
    /// <summary>
    /// Minifies file for the css minifier.
    /// </summary>
    /// <param name="inputPath">Path of the input to use.</param>
    /// <param name="outputPath">Path of the output to use.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the minify file operation is complete.</returns>
    ValueTask MinifyFile(string inputPath, string outputPath, CancellationToken cancellationToken = default);
}
