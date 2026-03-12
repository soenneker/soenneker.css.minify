using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Css.Minify.Abstract;

/// <summary>
/// A utility that minifies stylesheets
/// </summary>
public interface ICssMinifier
{
    string Minify(string css);
    string Minify(ReadOnlySpan<char> css);
    ValueTask MinifyFile(string inputPath, string outputPath, CancellationToken cancellationToken = default);
}
