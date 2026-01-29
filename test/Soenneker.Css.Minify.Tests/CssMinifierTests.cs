using System;
using System.IO;
using AwesomeAssertions;
using Soenneker.Css.Minify.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Css.Minify.Tests;

[Collection("Collection")]
public sealed class CssMinifierTests : FixturedUnitTest
{
    private readonly ICssMinifier _sut;

    public CssMinifierTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _sut = Resolve<ICssMinifier>(scoped: true);
    }

    [Fact]
    public void Minify_removes_comments_and_whitespace()
    {
        ICssMinifier sut = _sut;

        const string input = "/* comment */\nbody {\n  margin: 0px  ;\n  color : red ;\n}\n";
        const string expected = "body{margin:0;color:red}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_preserves_value_separators()
    {
        ICssMinifier sut = _sut;

        const string input = "h1{margin:0 .50em 1em 0px}";
        const string expected = "h1{margin:0 .5em 1em 0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_preserves_strings_and_calc_spacing()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { content: \"a /* not comment */ b\"; width: calc(100% - 1px); }";
        const string expected = ".a{content:\"a /* not comment */ b\";width:calc(100% - 1px)}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_media_queries()
    {
        ICssMinifier sut = _sut;

        const string input = "@media screen and (min-width: 600px) { .a { margin: 0px; } }";
        const string expected = "@media screen and (min-width: 600px){.a{margin:0}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_custom_properties_and_var()
    {
        ICssMinifier sut = _sut;

        const string input = ":root { --gap: 10px; } .a { margin: var(--gap); }";
        const string expected = ":root{--gap:10px}.a{margin:var(--gap)}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_preserves_url_strings()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { background-image: url(\"images/my file.png\"); }";
        const string expected = ".a{background-image:url(\"images/my file.png\")}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_keyframes()
    {
        ICssMinifier sut = _sut;

        const string input = "@keyframes fade { 0% { opacity: 0; } 50% { opacity: .5; } 100% { opacity: 1; } }";
        const string expected = "@keyframes fade{0%{opacity:0}50%{opacity:.5}100%{opacity:1}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_supports_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@supports (display: grid) { .a { display: grid; } }";
        const string expected = "@supports (display: grid){.a{display:grid}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_font_face()
    {
        ICssMinifier sut = _sut;

        const string input = "@font-face { font-family: \"MyFont\"; src: url(\"/fonts/myfont.woff2\") format(\"woff2\"); }";
        const string expected = "@font-face{font-family:\"MyFont\";src:url(\"/fonts/myfont.woff2\") format(\"woff2\")}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_preserves_descendant_selectors()
    {
        ICssMinifier sut = _sut;

        const string input = ".a .b, .c  .d { color: red; }";
        const string expected = ".a .b,.c .d{color:red}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_normalizes_numbers_and_zero_units()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { margin: 0px 00.50em -0.50em; line-height: 01.500; }";
        const string expected = ".a{margin:0 .5em -.5em;line-height:1.5}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_strips_comments_between_rules()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { color: red; } /* comment */ .b { color: blue; }";
        const string expected = ".a{color:red}.b{color:blue}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_multiple_selectors_and_combinators()
    {
        ICssMinifier sut = _sut;

        const string input = "ul > li + a, .x~.y { padding: 0; }";
        const string expected = "ul>li+a,.x~.y{padding:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_attribute_and_pseudo_selectors()
    {
        ICssMinifier sut = _sut;

        const string input = "a[href^=\"https\"]:not(.btn):hover { text-decoration: none; }";
        const string expected = "a[href^=\"https\"]:not(.btn):hover{text-decoration:none}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_important_and_multiple_values()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { border: 1px solid  #fff !important; }";
        const string expected = ".a{border:1px solid #fff!important}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_gradient_and_function_values()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { background: linear-gradient(90deg, #fff 0%, rgba(0, 0, 0, .5) 100%); }";
        const string expected = ".a{background:linear-gradient(90deg,#fff 0,rgba(0,0,0,.5) 100%)}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_var_fallback()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { color: var(--brand, rgb(0, 0, 0)); }";
        const string expected = ".a{color:var(--brand,rgb(0,0,0))}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_keyframes_from_to()
    {
        ICssMinifier sut = _sut;

        const string input = "@keyframes move { from { transform: translateX(0px); } to { transform: translateX(10px); } }";
        const string expected = "@keyframes move{from{transform:translateX(0)}to{transform:translateX(10px)}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_container_query()
    {
        ICssMinifier sut = _sut;

        const string input = "@container (min-width: 600px) { .a { display: block; } }";
        const string expected = "@container (min-width: 600px){.a{display:block}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_layer_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@layer utilities { .m-0 { margin: 0px; } }";
        const string expected = "@layer utilities{.m-0{margin:0}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_namespace_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@namespace svg url(\"http://www.w3.org/2000/svg\"); svg|a { fill: red; }";
        const string expected = "@namespace svg url(\"http://www.w3.org/2000/svg\");svg|a{fill:red}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_import_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@import url(\"theme.css\") screen and (min-width: 600px);";
        const string expected = "@import url(\"theme.css\") screen and (min-width: 600px);";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_page_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@page :first { margin: 1cm; }";
        const string expected = "@page :first{margin:1cm}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_property_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@property --x { syntax: \"<length>\"; inherits: false; initial-value: 0px; }";
        const string expected = "@property --x{syntax:\"<length>\";inherits:false;initial-value:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_counter_style_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@counter-style thumbs { system: cyclic; symbols: \"up\" \"down\"; suffix: \" \"; }";
        const string expected = "@counter-style thumbs{system:cyclic;symbols:\"up\" \"down\";suffix:\" \"}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_supports_selector()
    {
        ICssMinifier sut = _sut;

        const string input = "@supports selector(:is(.a, .b)) { .x { color: red; } }";
        const string expected = "@supports selector(:is(.a,.b)){.x{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_child_formula()
    {
        ICssMinifier sut = _sut;

        const string input = "li:nth-child(2n + 1) { margin: 0px; }";
        const string expected = "li:nth-child(2n+1){margin:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_unicode_range()
    {
        ICssMinifier sut = _sut;

        const string input = "@font-face { unicode-range: U+0025-00FF, U+4??; }";
        const string expected = "@font-face{unicode-range:U+0025-00FF,U+4??}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_preserves_data_uri()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { background-image: url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='10'/></svg>\"); }";
        const string expected = ".a{background-image:url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='10'/></svg>\")}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_escaped_identifiers()
    {
        ICssMinifier sut = _sut;

        const string input = ".\\31 0\\% { content: \"a\"; }";
        const string expected = ".\\31 0\\%{content:\"a\"}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nested_calc_and_var()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { width: calc(100% - var(--gap, 10px)); }";
        const string expected = ".a{width:calc(100% - var(--gap,10px))}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_font_feature_values()
    {
        ICssMinifier sut = _sut;

        const string input = "@font-feature-values FontOne { @styleset { nice-style: 1; } }";
        const string expected = "@font-feature-values FontOne{@styleset{nice-style:1}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_document_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@document url(\"https://example.com/\") { .a { color: red; } }";
        const string expected = "@document url(\"https://example.com/\"){.a{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_starting_style_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@starting-style { .a { opacity: 0; } }";
        const string expected = "@starting-style{.a{opacity:0}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_last_child_formula()
    {
        ICssMinifier sut = _sut;

        const string input = "li:nth-last-child(2n - 1) { padding: 0px; }";
        const string expected = "li:nth-last-child(2n - 1){padding:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_has_and_where_selectors()
    {
        ICssMinifier sut = _sut;

        const string input = ".card:has(img):where(.large) { border: 0px; }";
        const string expected = ".card:has(img):where(.large){border:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_media_nesting()
    {
        ICssMinifier sut = _sut;

        const string input = "@media screen { @media (min-width: 600px) { .a { display: block; } } }";
        const string expected = "@media screen{@media(min-width:600px){.a{display:block}}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_layer_ordering()
    {
        ICssMinifier sut = _sut;

        const string input = "@layer reset, base, utilities;";
        const string expected = "@layer reset,base,utilities;";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_scope_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@scope (.card) { .title { font-weight: 700; } }";
        const string expected = "@scope (.card){.title{font-weight:700}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_container_style_query()
    {
        ICssMinifier sut = _sut;

        const string input = "@container style(--size: large) { .a { color: red; } }";
        const string expected = "@container style(--size: large){.a{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_supports_not()
    {
        ICssMinifier sut = _sut;

        const string input = "@supports not (display: grid) { .a { display: block; } }";
        const string expected = "@supports not (display: grid){.a{display:block}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_media_complex_conditions()
    {
        ICssMinifier sut = _sut;

        const string input = "@media screen and (min-width: 600px) and (orientation: landscape) { .a { display: block; } }";
        const string expected = "@media screen and (min-width: 600px) and (orientation: landscape){.a{display:block}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_container_and_or()
    {
        ICssMinifier sut = _sut;

        const string input = "@container (min-width: 30em) and (max-width: 60em) { .a { display: block; } }";
        const string expected = "@container (min-width: 30em) and (max-width: 60em){.a{display:block}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_is_and_where_selectors()
    {
        ICssMinifier sut = _sut;

        const string input = ":is(.a, .b, .c):where(.x, .y) { color: red; }";
        const string expected = ":is(.a,.b,.c):where(.x,.y){color:red}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_url_without_quotes()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { background: url(images/bg.png); }";
        const string expected = ".a{background:url(images/bg.png)}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_data_uri_without_quotes()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { background: url(data:image/svg+xml;base64,PHN2Zy8+); }";
        const string expected = ".a{background:url(data:image/svg+xml;base64,PHN2Zy8+)}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_viewport_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@viewport { width: device-width; zoom: 1.0; }";
        const string expected = "@viewport{width:device-width;zoom:1}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_page_margin_boxes()
    {
        ICssMinifier sut = _sut;

        const string input = "@page { @top-left { content: \"Title\"; } }";
        const string expected = "@page{@top-left{content:\"Title\"}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_escaped_unicode_identifier()
    {
        ICssMinifier sut = _sut;

        const string input = ".\\26 a { color: red; }";
        const string expected = ".\\26 a{color:red}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_media_not_only()
    {
        ICssMinifier sut = _sut;

        const string input = "@media not screen and (max-width: 800px) { .a { display: none; } }";
        const string expected = "@media not screen and (max-width: 800px){.a{display:none}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_container_style_and()
    {
        ICssMinifier sut = _sut;

        const string input = "@container style(--size: large) and (min-width: 30em) { .a { color: red; } }";
        const string expected = "@container style(--size: large) and (min-width: 30em){.a{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_of_type()
    {
        ICssMinifier sut = _sut;

        const string input = "p:nth-of-type(2n + 3) { margin: 0px; }";
        const string expected = "p:nth-of-type(2n+3){margin:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_last_of_type()
    {
        ICssMinifier sut = _sut;

        const string input = "p:nth-last-of-type(2n - 1) { padding: 0px; }";
        const string expected = "p:nth-last-of-type(2n - 1){padding:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_lang_and_dir()
    {
        ICssMinifier sut = _sut;

        const string input = "html:lang(en):dir(ltr) { font-family: serif; }";
        const string expected = "html:lang(en):dir(ltr){font-family:serif}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_supports_selector_function()
    {
        ICssMinifier sut = _sut;

        const string input = "@supports selector(:has(> img)) { .a { color: red; } }";
        const string expected = "@supports selector(:has(>img)){.a{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_counter_style_descriptors()
    {
        ICssMinifier sut = _sut;

        const string input = "@counter-style custom { system: fixed; symbols: a b c; negative: \"-\"; }";
        const string expected = "@counter-style custom{system:fixed;symbols:a b c;negative:\"-\"}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_font_feature_values_blocks()
    {
        ICssMinifier sut = _sut;

        const string input = "@font-feature-values FontTwo { @styleset { nice: 1; } @swash { cool: 2; } }";
        const string expected = "@font-feature-values FontTwo{@styleset{nice:1}@swash{cool:2}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_media_functions()
    {
        ICssMinifier sut = _sut;

        const string input = "@media (width > 600px) { .a { display: block; } }";
        const string expected = "@media (width>600px){.a{display:block}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_supports_and_or_groups()
    {
        ICssMinifier sut = _sut;

        const string input = "@supports ((display: grid) and (not (display: flex))) or (display: block) { .a { color: red; } }";
        const string expected = "@supports ((display: grid) and (not (display: flex))) or (display: block){.a{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_supports_selector_not()
    {
        ICssMinifier sut = _sut;

        const string input = "@supports not selector(:has(> img)) { .a { color: red; } }";
        const string expected = "@supports not selector(:has(>img)){.a{color:red}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_layer_nested_blocks()
    {
        ICssMinifier sut = _sut;

        const string input = "@layer base { @layer typography { h1 { margin: 0px; } } }";
        const string expected = "@layer base{@layer typography{h1{margin:0}}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_scope_with_to_selector()
    {
        ICssMinifier sut = _sut;

        const string input = "@scope (.card) to (.card .footer) { .title { font-weight: 700; } }";
        const string expected = "@scope (.card) to (.card .footer){.title{font-weight:700}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_child_of_syntax()
    {
        ICssMinifier sut = _sut;

        const string input = "li:nth-child(2n - 1 of .a, .b) { margin: 0px; }";
        const string expected = "li:nth-child(2n - 1 of .a,.b){margin:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_not_selector_list()
    {
        ICssMinifier sut = _sut;

        const string input = "a:not(.btn, .link) { color: red; }";
        const string expected = "a:not(.btn,.link){color:red}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_focus_visible_and_placeholder()
    {
        ICssMinifier sut = _sut;

        const string input = "input:focus-visible::placeholder { color: #999; }";
        const string expected = "input:focus-visible::placeholder{color:#999}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_font_face_descriptors()
    {
        ICssMinifier sut = _sut;

        const string input = "@font-face { font-family: \"Inter\"; font-weight: 100 900; font-display: swap; }";
        const string expected = "@font-face{font-family:\"Inter\";font-weight:100 900;font-display:swap}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_keyframes_steps_function()
    {
        ICssMinifier sut = _sut;

        const string input = "@keyframes jump { from { animation-timing-function: steps(4, end); } to { animation-timing-function: steps(4, start); } }";
        const string expected = "@keyframes jump{from{animation-timing-function:steps(4,end)}to{animation-timing-function:steps(4,start)}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_color_profile_rule()
    {
        ICssMinifier sut = _sut;

        const string input = "@color-profile srgb { src: url(\"sRGB.icc\"); rendering-intent: perceptual; }";
        const string expected = "@color-profile srgb{src:url(\"sRGB.icc\");rendering-intent:perceptual}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_container_not()
    {
        ICssMinifier sut = _sut;

        const string input = "@container not (min-width: 30em) { .a { display: none; } }";
        const string expected = "@container not (min-width: 30em){.a{display:none}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_media_only_and_comma_list()
    {
        ICssMinifier sut = _sut;

        const string input = "@media only screen and (min-width: 600px), print { .a { display: block; } }";
        const string expected = "@media only screen and (min-width: 600px),print{.a{display:block}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_child_keywords()
    {
        ICssMinifier sut = _sut;

        const string input = "li:nth-child(odd) { margin: 0px; } li:nth-child(even) { margin: 0px; }";
        const string expected = "li:nth-child(odd){margin:0}li:nth-child(even){margin:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_nth_of_type_of_syntax()
    {
        ICssMinifier sut = _sut;

        const string input = "p:nth-of-type(2n of .a, .b) { padding: 0px; }";
        const string expected = "p:nth-of-type(2n of .a,.b){padding:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_counter_style_additive_symbols()
    {
        ICssMinifier sut = _sut;

        const string input = "@counter-style additive { system: additive; additive-symbols: 1000 \"M\", 500 \"D\"; }";
        const string expected = "@counter-style additive{system:additive;additive-symbols:1000 \"M\",500 \"D\"}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_page_named_and_pseudo()
    {
        ICssMinifier sut = _sut;

        const string input = "@page chapter:first { margin: 1cm; }";
        const string expected = "@page chapter:first{margin:1cm}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_scope_to_combinators()
    {
        ICssMinifier sut = _sut;

        const string input = "@scope (.card) to (.card > .footer) { .title { font-weight: 700; } }";
        const string expected = "@scope (.card) to (.card>.footer){.title{font-weight:700}}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_has_complex_selector_list()
    {
        ICssMinifier sut = _sut;

        const string input = "article:has(> img, > video) { margin: 0px; }";
        const string expected = "article:has(>img,>video){margin:0}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_url_with_spaces_and_escapes()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { background-image: url(\"images/space\\ 20.png\"); }";
        const string expected = ".a{background-image:url(\"images/space\\ 20.png\")}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Minify_handles_calc_min_max_clamp()
    {
        ICssMinifier sut = _sut;

        const string input = ".a { width: clamp(10px, calc(5vw + 1rem), max(100px, 50%)); }";
        const string expected = ".a{width:clamp(10px,calc(5vw + 1rem),max(100px,50%))}";

        string result = sut.Minify(input);

        result.Should().Be(expected);
    }

    [Fact]
    public async System.Threading.Tasks.ValueTask MinifyFile_writes_output()
    {
        ICssMinifier sut = _sut;
        var cancellationToken = TestContext.Current.CancellationToken;

        string tempDir = Path.Combine(Path.GetTempPath(), $"cssminify-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        string inputPath = Path.Combine(tempDir, "input.css");
        string outputPath = Path.Combine(tempDir, "output.css");

        try
        {
            const string input = "/* comment */\nbody {\n  margin: 0px  ;\n  color : red ;\n}\n";
            const string expected = "body{margin:0;color:red}";

            await File.WriteAllTextAsync(inputPath, input, cancellationToken);

            await sut.MinifyFile(inputPath, outputPath, cancellationToken);

            string result = await File.ReadAllTextAsync(outputPath, cancellationToken);

            result.Should().Be(expected);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; test output is the primary concern.
            }
        }
    }
}
