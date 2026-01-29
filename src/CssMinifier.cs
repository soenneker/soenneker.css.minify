using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Css.Minify.Abstract;
using Soenneker.Extensions.Task;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.PooledStringBuilders;

namespace Soenneker.Css.Minify;

/// <inheritdoc cref="ICssMinifier"/>
public sealed class CssMinifier : ICssMinifier
{
    private readonly IFileUtil _fileUtil;

    public CssMinifier(IFileUtil fileUtil)
    {
        _fileUtil = fileUtil ?? throw new ArgumentNullException(nameof(fileUtil));
    }

    public string Minify(string css) => css.Length == 0 ? string.Empty : Minify(css.AsSpan());

    public string Minify(ReadOnlySpan<char> css)
    {
        if (css.IsEmpty)
            return string.Empty;

        var capacity = Math.Max(32, css.Length);

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
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("Input path must be provided", nameof(inputPath));

        if (string.IsNullOrWhiteSpace(outputPath))
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

        void UpdatePrev(char value)
        {
            if (value == '\0')
                return;

            prevPrevNonWhitespace = prevNonWhitespace;
            prevNonWhitespace = value;
        }

        for (int i = 0; i < css.Length; i++)
        {
            char c = css[i];
            char next = i + 1 < css.Length ? css[i + 1] : '\0';

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

                if (c == '\\' && next != '\0')
                {
                    sb.Append(next);
                    i++;
                    UpdatePrev(next);
                    continue;
                }

                if (c == stringQuote)
                    inString = false;

                UpdatePrev(c);
                continue;
            }

            if (c == '/' && next == '*')
            {
                inComment = true;
                i++;
                continue;
            }

            if (inUnicodeRangeToken && IsUnicodeRangeTerminator(c))
                inUnicodeRangeToken = false;

            if (char.IsWhiteSpace(c))
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
                sb.Append("calc(");
                inCalc = true;
                calcParenDepth = 1;
                UpdatePrev('(');
                i += 4;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                sb.Append(c);
                inString = true;
                stringQuote = c;
                UpdatePrev(c);
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

            if (c == ';' && (next == '}' || IsSemicolonBeforeBlockEnd(css, i)))
                continue;

            if (inUnicodeRangeToken == false && IsNumberStart(css, i, prevNonWhitespace, prevPrevNonWhitespace))
            {
                i = AppendNormalizedNumber(css, i, ref sb, out char lastAppended);
                if (lastAppended != '\0')
                    UpdatePrev(lastAppended);
                continue;
            }

            sb.Append(c);
            UpdatePrev(c);

            if (!inUnicodeRangeToken && (c is 'U' or 'u') && next == '+')
                inUnicodeRangeToken = true;
        }
    }

    private static bool IsCalcStart(ReadOnlySpan<char> css, int index)
    {
        if (index + 4 >= css.Length)
            return false;

        char c0 = css[index];
        if (c0 is not ('c' or 'C'))
            return false;

        if (!IsCharEqualIgnoreCase(css[index + 1], 'a') ||
            !IsCharEqualIgnoreCase(css[index + 2], 'l') ||
            !IsCharEqualIgnoreCase(css[index + 3], 'c') ||
            css[index + 4] != '(')
            return false;

        if (index > 0 && IsIdentChar(css[index - 1]))
            return false;

        return true;
    }

    private static bool IsCharEqualIgnoreCase(char a, char b) =>
        a == b || char.ToUpperInvariant(a) == char.ToUpperInvariant(b);

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

    private static bool IsNoSpaceBefore(char c, bool inBlock)
    {
        if (c is ',' or ';' or ')' or ']' or '}' or '{' or '>' or '+' or '~' or '=')
            return true;

        if (inBlock && c == ':')
            return true;

        return false;
    }

    private static bool IsNoSpaceAfter(char c, bool inBlock)
    {
        if (c is ',' or '(' or '[' or '{' or '}' or '>' or '+' or '~' or '=')
            return true;

        if (inBlock && c == ':')
            return true;

        return false;
    }

    private static bool IsIdentChar(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '-' or '\\';

    private static bool IsUnicodeRangeTerminator(char c) =>
        char.IsWhiteSpace(c) || c is ',' or ';' or ')' or '}' or '{';

    private static bool IsSemicolonBeforeBlockEnd(ReadOnlySpan<char> css, int semicolonIndex)
    {
        for (int i = semicolonIndex + 1; i < css.Length; i++)
        {
            char c = css[i];

            if (char.IsWhiteSpace(c))
                continue;

            if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < css.Length && !(css[i] == '*' && css[i + 1] == '/'))
                    i++;

                if (i + 1 < css.Length)
                    i++;

                continue;
            }

            return c == '}';
        }

        return false;
    }

    private static bool IsValueTokenChar(char c) =>
        char.IsLetterOrDigit(c) || c is '%' or ')' or ']' or '"' or '\'' or '.' or '#';

    private static bool IsValueTokenStart(char c) =>
        char.IsLetterOrDigit(c) || c is '.' or '-' or '+' or '#' or '"' or '\'';

    private static bool IsNumberStart(ReadOnlySpan<char> css, int index, char prevNonWhitespace, char prevPrevNonWhitespace)
    {
        char c = css[index];
        char next = index + 1 < css.Length ? css[index + 1] : '\0';

        if (prevNonWhitespace == '+' && (prevPrevNonWhitespace is 'U' or 'u'))
            return false;

        if (char.IsDigit(c))
            return true;

        if (c == '.' && char.IsDigit(next))
            return true;

        if ((c == '-' || c == '+') && (char.IsDigit(next) || next == '.'))
        {
            if (prevNonWhitespace == '\0' || IsDelimiter(prevNonWhitespace))
                return true;
        }

        return false;
    }

    private static bool IsDelimiter(char c) =>
        char.IsWhiteSpace(c) || c is ':' or ',' or ';' or '(' or '{' or '[' or '!' or '=' or '>' or '+' or '-' or '*' or '/' or '~';

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
        while (i < len && char.IsDigit(css[i]))
            i++;

        int intEnd = i;
        bool hasDot = i < len && css[i] == '.';

        int fracStart = 0;
        int fracEnd = 0;

        if (hasDot)
        {
            i++;
            fracStart = i;
            while (i < len && char.IsDigit(css[i]))
                i++;
            fracEnd = i;
        }

        int numberEnd = i;

        if (numberEnd == intStart && !hasDot)
            return startIndex;

        int unitStart = i;
        while (i < len && (char.IsLetter(css[i]) || css[i] == '%'))
            i++;
        int unitEnd = i;

        ReadOnlySpan<char> intPart = css.Slice(intStart, intEnd - intStart);
        ReadOnlySpan<char> fracPart = hasDot ? css.Slice(fracStart, fracEnd - fracStart) : ReadOnlySpan<char>.Empty;

        bool isZero = IsAllZeros(intPart) && IsAllZeros(fracPart);
        bool hasUnit = unitEnd > unitStart;
        bool isPercentUnit = hasUnit && css[unitStart] == '%';
        char nextSig = NextSignificantChar(css, unitEnd);

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

    private static bool IsAllZeros(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] != '0')
                return false;
        }

        return true;
    }

    private static ReadOnlySpan<char> TrimLeadingZeros(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length && span[i] == '0')
            i++;

        return span.Slice(i);
    }

    private static ReadOnlySpan<char> TrimTrailingZeros(ReadOnlySpan<char> span)
    {
        int i = span.Length - 1;
        while (i >= 0 && span[i] == '0')
            i--;

        return span.Slice(0, i + 1);
    }

    private static char NextSignificantChar(ReadOnlySpan<char> css, int startIndex)
    {
        for (int i = startIndex; i < css.Length; i++)
        {
            char c = css[i];

            if (char.IsWhiteSpace(c))
                continue;

            if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < css.Length && !(css[i] == '*' && css[i + 1] == '/'))
                    i++;

                if (i + 1 < css.Length)
                    i++;

                continue;
            }

            return c;
        }

        return '\0';
    }
}
