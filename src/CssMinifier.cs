using Soenneker.Css.Minify.Abstract;
using Soenneker.Extensions.Char;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.PooledStringBuilders;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Css.Minify;

/// <inheritdoc cref="ICssMinifier"/>
public sealed class CssMinifier : ICssMinifier
{
    private readonly IFileUtil _fileUtil;

    public CssMinifier(IFileUtil fileUtil)
    {
        _fileUtil = fileUtil;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string Minify(string css) => css.Length == 0 ? string.Empty : Minify(css.AsSpan());

    public string Minify(ReadOnlySpan<char> css)
    {
        if (css.IsEmpty)
            return string.Empty;

        // Capacity heuristic: try to avoid growth for common cases.
        int capacity = css.Length < 32 ? 32 : css.Length;

        var sb = new PooledStringBuilder(capacity);

        try
        {
            MinifyInto(css, ref sb);
            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    public async ValueTask MinifyFile(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        if (inputPath.IsNullOrWhiteSpace())
            throw new ArgumentException("Input path must be provided", nameof(inputPath));

        if (outputPath.IsNullOrWhiteSpace())
            throw new ArgumentException("Output path must be provided", nameof(outputPath));

        string css = await _fileUtil.Read(inputPath, cancellationToken: cancellationToken).NoSync();
        string minified = Minify(css);
        await _fileUtil.Write(outputPath, minified, cancellationToken: cancellationToken).NoSync();
    }

    private static void MinifyInto(ReadOnlySpan<char> css, ref PooledStringBuilder sb)
    {
        bool inString = false;
        char stringQuote = '\0';
        bool inComment = false;
        bool pendingSpace = false;

        bool inCalc = false;
        int calcParenDepth = 0;

        int blockDepth = 0;

        char prevNonWhitespace = '\0';
        char prevPrevNonWhitespace = '\0';

        bool inUnicodeRangeToken = false;

        int len = css.Length;

        for (int i = 0; i < len; i++)
        {
            char c = css[i];
            char next = (i + 1 < len) ? css[i + 1] : '\0';

            if (inComment)
            {
                if (c == '*' && next == '/')
                {
                    inComment = false;
                    i++;
                }

                continue;
            }

            if (inString)
            {
                sb.Append(c);

                // Preserve escapes as-is.
                if (c == '\\' && next != '\0')
                {
                    sb.Append(next);
                    i++;

                    // Update prevs with escaped char as the last "significant" one.
                    if (next != '\0')
                    {
                        prevPrevNonWhitespace = prevNonWhitespace;
                        prevNonWhitespace = next;
                    }

                    continue;
                }

                if (c == stringQuote)
                    inString = false;

                if (c != '\0')
                {
                    prevPrevNonWhitespace = prevNonWhitespace;
                    prevNonWhitespace = c;
                }

                continue;
            }

            // Comment start
            if (c == '/' && next == '*')
            {
                inComment = true;
                i++;
                continue;
            }

            if (inUnicodeRangeToken && IsUnicodeRangeTerminator(c))
                inUnicodeRangeToken = false;

            if (c.IsAsciiWhiteSpace())
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace)
            {
                if (ShouldEmitSpace(prevNonWhitespace, c, blockDepth > 0, inCalc))
                    sb.Append(' ');

                pendingSpace = false;
            }

            if (IsCalcStart(css, i))
            {
                // Avoid per-call allocations; literal is interned.
                sb.Append("calc(");
                inCalc = true;
                calcParenDepth = 1;

                prevPrevNonWhitespace = prevNonWhitespace;
                prevNonWhitespace = '(';

                i += 4; // we consumed "calc("
                continue;
            }

            if (c is '"' or '\'')
            {
                sb.Append(c);
                inString = true;
                stringQuote = c;

                prevPrevNonWhitespace = prevNonWhitespace;
                prevNonWhitespace = c;

                continue;
            }

            if (c == '{')
                blockDepth++;
            else if (c == '}' && blockDepth > 0)
                blockDepth--;

            if (inCalc)
            {
                if (c == '(')
                    calcParenDepth++;
                else if (c == ')')
                {
                    calcParenDepth--;
                    if (calcParenDepth == 0)
                        inCalc = false;
                }
            }

            // Drop unnecessary ; before block end
            if (c == ';' && (next == '}' || IsSemicolonBeforeBlockEnd(css, i)))
                continue;

            if (!inUnicodeRangeToken && IsNumberStart(css, i, prevNonWhitespace, prevPrevNonWhitespace))
            {
                i = AppendNormalizedNumber(css, i, ref sb, out char lastAppended);

                if (lastAppended != '\0')
                {
                    prevPrevNonWhitespace = prevNonWhitespace;
                    prevNonWhitespace = lastAppended;
                }

                continue;
            }

            sb.Append(c);

            if (c != '\0')
            {
                prevPrevNonWhitespace = prevNonWhitespace;
                prevNonWhitespace = c;
            }

            if (!inUnicodeRangeToken && (c is 'U' or 'u') && next == '+')
                inUnicodeRangeToken = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCalcStart(ReadOnlySpan<char> css, int index)
    {
        // Need "calc(" => 5 chars; we check index+4
        if ((uint)(index + 4) >= (uint)css.Length)
            return false;

        // Case-insensitive ASCII match: c a l c (
        if (!IsAsciiCharEqualIgnoreCase(css[index], 'c') ||
            !IsAsciiCharEqualIgnoreCase(css[index + 1], 'a') ||
            !IsAsciiCharEqualIgnoreCase(css[index + 2], 'l') ||
            !IsAsciiCharEqualIgnoreCase(css[index + 3], 'c') ||
            css[index + 4] != '(')
        {
            return false;
        }

        // Avoid matching "mycalc(" as an ident continuation
        if (index > 0 && IsIdentChar(css[index - 1]))
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiCharEqualIgnoreCase(char a, char bLower)
    {
        // bLower must be lowercase ASCII letter for this to be valid.
        // For non-letters, this still works as equality check via fast path.
        char x = a;
        if ((uint)(x - 'A') <= ('Z' - 'A'))
            x = (char)(x | 0x20);

        return x == bLower;
    }

    private static bool ShouldEmitSpace(char prev, char current, bool inBlock, bool inCalc)
    {
        if (prev == '\0')
            return false;

        if (prev == ';')
            return false;

        if (inCalc && (prev is '+' or '-' || current is '+' or '-'))
            return true;

        if (IsNoSpaceAfter(prev, inBlock) || IsNoSpaceBefore(current, inBlock))
            return false;

        if (!inBlock)
            return true;

        if (IsIdentChar(prev) && IsIdentChar(current))
            return true;

        if (IsIdentChar(prev) && current == '#')
            return true;

        if (IsValueTokenChar(prev) && IsValueTokenStart(current))
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNoSpaceBefore(char c, bool inBlock)
    {
        if (c is ',' or ';' or ')' or ']' or '}' or '{' or '>' or '+' or '~' or '=')
            return true;

        if (inBlock && c == ':')
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNoSpaceAfter(char c, bool inBlock)
    {
        if (c is ',' or '(' or '[' or '{' or '}' or '>' or '+' or '~' or '=')
            return true;

        if (inBlock && c == ':')
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIdentChar(char c)
    {
        // ASCII fast path (most CSS)
        if (c.IsAsciiAlphaNum())
            return true;

        return c is '_' or '-' or '\\' || char.IsLetterOrDigit(c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnicodeRangeTerminator(char c) =>
        c.IsAsciiWhiteSpace() || c is ',' or ';' or ')' or '}' or '{';

    private static bool IsSemicolonBeforeBlockEnd(ReadOnlySpan<char> css, int semicolonIndex)
    {
        int len = css.Length;

        for (int i = semicolonIndex + 1; i < len; i++)
        {
            char c = css[i];

            if (c.IsAsciiWhiteSpace())
                continue;

            if (c == '/' && i + 1 < len && css[i + 1] == '*')
            {
                i = SkipBlockComment(css, i);
                continue;
            }

            return c == '}';
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValueTokenChar(char c) =>
        c.IsAsciiAlphaNum() || c is '%' or ')' or ']' or '"' or '\'' or '.' or '#';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValueTokenStart(char c) =>
        c.IsAsciiAlphaNum() || c is '.' or '-' or '+' or '#' or '"' or '\'';

    private static bool IsNumberStart(ReadOnlySpan<char> css, int index, char prevNonWhitespace, char prevPrevNonWhitespace)
    {
        char c = css[index];
        char next = (index + 1 < css.Length) ? css[index + 1] : '\0';

        // Prevent "U+10-1F" etc. from being treated as numeric start after U+
        if (prevNonWhitespace == '+' && (prevPrevNonWhitespace is 'U' or 'u'))
            return false;

        if (c.IsAsciiDigit() || char.IsDigit(c))
            return true;

        if (c == '.' && (next.IsAsciiDigit() || char.IsDigit(next)))
            return true;

        if ((c == '-' || c == '+') && (next.IsAsciiDigit() || next == '.' || char.IsDigit(next)))
        {
            if (prevNonWhitespace == '\0' || IsDelimiter(prevNonWhitespace))
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDelimiter(char c) =>
        c.IsAsciiWhiteSpace() || c is ':' or ',' or ';' or '(' or '{' or '[' or '!' or '=' or '>' or '+' or '-' or '*' or '/' or '~';

    private static int AppendNormalizedNumber(ReadOnlySpan<char> css, int startIndex, ref PooledStringBuilder sb, out char lastAppended)
    {
        lastAppended = '\0';

        int i = startIndex;
        int len = css.Length;

        char signChar = '\0';
        if (css[i] is '+' or '-')
        {
            signChar = css[i];
            i++;
        }

        int intStart = i;

        while (i < len && css[i].IsDigit())
            i++;

        int intEnd = i;

        bool hasDot = i < len && css[i] == '.';

        int fracStart = 0;
        int fracEnd = 0;

        if (hasDot)
        {
            i++;
            fracStart = i;

            while (i < len && css[i].IsDigit())
                i++;

            fracEnd = i;
        }

        int numberEnd = i;

        if (numberEnd == intStart && !hasDot)
            return startIndex;

        int unitStart = i;

        while (i < len && IsUnitChar(css[i]))
            i++;

        int unitEnd = i;

        ReadOnlySpan<char> intPart = css.Slice(intStart, intEnd - intStart);
        ReadOnlySpan<char> fracPart = hasDot ? css.Slice(fracStart, fracEnd - fracStart) : ReadOnlySpan<char>.Empty;

        bool isZero = IsAllZeros(intPart) && IsAllZeros(fracPart);
        bool hasUnit = unitEnd > unitStart;
        bool isPercentUnit = hasUnit && css[unitStart] == '%';
        char nextSig = NextSignificantChar(css, unitEnd);

        // Keep "0%" if directly before '{' (unicode-range / edge cases)
        if (isZero && isPercentUnit && nextSig == '{')
        {
            sb.Append('0');
            sb.Append('%');
            lastAppended = '%';
            return unitEnd - 1;
        }

        if (isZero)
        {
            sb.Append('0');
            lastAppended = '0';
            return unitEnd - 1;
        }

        intPart = TrimLeadingZeros(intPart);
        fracPart = TrimTrailingZeros(fracPart);

        bool negative = signChar == '-';

        if (fracPart.IsEmpty)
        {
            if (intPart.IsEmpty)
                intPart = "0";

            if (negative)
            {
                sb.Append('-');
                lastAppended = '-';
            }

            sb.Append(intPart);
            if (intPart.Length > 0)
                lastAppended = intPart[^1];
        }
        else if (intPart.IsEmpty)
        {
            if (negative)
            {
                sb.Append('-');
                lastAppended = '-';
            }

            sb.Append('.');
            sb.Append(fracPart);

            if (fracPart.Length > 0)
                lastAppended = fracPart[^1];
        }
        else
        {
            if (negative)
            {
                sb.Append('-');
                lastAppended = '-';
            }

            sb.Append(intPart);
            sb.Append('.');
            sb.Append(fracPart);

            if (fracPart.Length > 0)
                lastAppended = fracPart[^1];
        }

        if (hasUnit)
        {
            sb.Append(css.Slice(unitStart, unitEnd - unitStart));
            lastAppended = css[unitEnd - 1];
        }

        return unitEnd - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAllZeros(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] != '0')
                return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> TrimLeadingZeros(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length && span[i] == '0')
            i++;

        return span.Slice(i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> TrimTrailingZeros(ReadOnlySpan<char> span)
    {
        int i = span.Length - 1;
        while (i >= 0 && span[i] == '0')
            i--;

        return span.Slice(0, i + 1);
    }

    private static char NextSignificantChar(ReadOnlySpan<char> css, int startIndex)
    {
        int len = css.Length;

        for (int i = startIndex; i < len; i++)
        {
            char c = css[i];

            if (c.IsAsciiWhiteSpace())
                continue;

            if (c == '/' && i + 1 < len && css[i + 1] == '*')
            {
                i = SkipBlockComment(css, i);
                continue;
            }

            return c;
        }

        return '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SkipBlockComment(ReadOnlySpan<char> css, int slashIndex)
    {
        // assumes css[slashIndex] == '/' and next is '*'
        int len = css.Length;
        int i = slashIndex + 2;

        while (i + 1 < len && !(css[i] == '*' && css[i + 1] == '/'))
            i++;

        if (i + 1 < len)
            i++; // position on '/'

        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnitChar(char c)
    {
        // Units are typically ASCII letters or '%'. Keep a fallback for odd unicode units.
        if (c == '%')
            return true;

        return c.IsAsciiLetter() || char.IsLetter(c);
    }
}
