using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Css.Minify.Abstract;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Esbuild.Util.Dtos;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.File.Abstract;

namespace Soenneker.Css.Minify;

public sealed class CssMinifier : ICssMinifier
{
    private readonly IEsbuildUtil _esbuildUtil;
    private readonly IFileUtil _fileUtil;

    public CssMinifier(IEsbuildUtil esbuildUtil, IFileUtil fileUtil)
    {
        _esbuildUtil = esbuildUtil;
        _fileUtil = fileUtil;
    }

    public ValueTask<string> Minify(string? css, CancellationToken cancellationToken = default) =>
        MinifyCore(css, null, cancellationToken);

    public async ValueTask MinifyFile(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        string css = await _fileUtil.Read(inputPath, cancellationToken: cancellationToken).NoSync();
        string minified = await MinifyCore(css, inputPath, cancellationToken).NoSync();
        await _fileUtil.Write(outputPath, minified, cancellationToken: cancellationToken).NoSync();
    }

    private async ValueTask<string> MinifyCore(string? css, string? sourcefile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(css))
            return string.Empty;

        EsbuildCommandResult result = await _esbuildUtil.Transform(css, new EsbuildTransformOptions
        {
            Loader = "css",
            Minify = true,
            Sourcefile = sourcefile
        }, cancellationToken).NoSync();
        return result.StandardOutput;
    }
}
