using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Css.Minify.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Css.Minify.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class CssMinifierTests : HostedUnitTest
{
    private readonly ICssMinifier _sut;

    public CssMinifierTests(Host host) : base(host)
    {
        _sut = Resolve<ICssMinifier>(scoped: true);
    }

    [Test]
    public async Task Minify_removes_comments_and_whitespace()
    {
        string result = await _sut.Minify("/* comment */ body { margin: 0px; color: red; }");
        result.Trim().Should().Be("body{margin:0;color:red}");
    }

    [Test]
    public async Task Minify_preserves_strings_and_calc_spacing()
    {
        string result = await _sut.Minify(".a { content: \"a /* not comment */ b\"; width: calc(100% - 1px); }");
        result.Trim().Should().Be(".a{content:\"a /* not comment */ b\";width:calc(100% - 1px)}");
    }

    [Test]
    public async Task Minify_preserves_custom_property_tokens_and_descendant_selectors()
    {
        string result = await _sut.Minify(":root { --gap: 001px; } .a .b { margin: var(--gap); }");
        result.Trim().Should().Be(":root{--gap: 001px}.a .b{margin:var(--gap)}");
    }

    [Test]
    public async Task Minify_does_not_resolve_imports_or_urls()
    {
        string result = await _sut.Minify("@import 'missing.css'; .a { background: url(missing.png); }");
        result.Should().Contain("missing.css").And.Contain("url(missing.png)");
    }

    [Test]
    public async Task Minify_empty_input_returns_empty()
    {
        (await _sut.Minify(null)).Should().BeEmpty();
        (await _sut.Minify(string.Empty)).Should().BeEmpty();
        (await _sut.Minify(" \r\n\t")).Should().BeEmpty();
    }

    [Test]
    public async Task Minify_honors_cancellation_even_for_empty_input()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Func<Task> action = async () => await _sut.Minify(string.Empty, source.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task MinifyFile_writes_output()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"cssminify-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string inputPath = Path.Combine(directory, "input.css");
            string outputPath = Path.Combine(directory, "output.css");
            await File.WriteAllTextAsync(inputPath, "body { margin: 0px; color: red; }");
            await _sut.MinifyFile(inputPath, outputPath);
            (await File.ReadAllTextAsync(outputPath)).Trim().Should().Be("body{margin:0;color:red}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task MinifyFile_rejects_empty_paths()
    {
        Func<Task> input = async () => await _sut.MinifyFile(" ", "output.css");
        Func<Task> output = async () => await _sut.MinifyFile("input.css", " ");
        await input.Should().ThrowAsync<ArgumentException>();
        await output.Should().ThrowAsync<ArgumentException>();
    }
}
