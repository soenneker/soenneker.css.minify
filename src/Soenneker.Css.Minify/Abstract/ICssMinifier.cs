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
    /// Executes the minify operation.
    /// </summary>
    /// <param name="css">The css.</param>
    /// <returns>The result of the operation.</returns>
    string Minify(string css);
    /// <summary>
    /// Executes the minify operation.
    /// </summary>
    /// <param name="css">The css.</param>
    /// <returns>The result of the operation.</returns>
    string Minify(ReadOnlySpan<char> css);
    /// <summary>
    /// Executes the minify file operation.
    /// </summary>
    /// <param name="inputPath">The input path.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask MinifyFile(string inputPath, string outputPath, CancellationToken cancellationToken = default);
}
