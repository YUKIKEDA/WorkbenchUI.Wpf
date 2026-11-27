using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Gallery.Controls;

/// <summary>Target programming language for <see cref="CodeSyntaxHighlighter"/>.</summary>
public enum CodeLanguage
{
    Xaml,
    CSharp,
}

/// <summary>
/// Lightweight, zero-dependency syntax highlighter for XAML and C# snippets displayed in <see cref="ControlExample"/>.
/// </summary>
public static class CodeSyntaxHighlighter
{
    private sealed record Palette(
        Brush Plain,
        Brush Comment,
        Brush KeywordOrTag,
        Brush IdentifierOrAttribute,
        Brush String,
        Brush Number,
        Brush Punctuation,
        Brush Type);

    private static readonly Palette DarkPalette = CreatePalette(
        plain: "#D4D4D4",
        comment: "#6A9955",
        keywordOrTag: "#569CD6",
        identifierOrAttribute: "#9CDCFE",
        str: "#CE9178",
        number: "#B5CEA8",
        punctuation: "#808080",
        type: "#4EC9B0");

    private static readonly Palette LightPalette = CreatePalette(
        plain: "#1E1E1E",
        comment: "#008000",
        keywordOrTag: "#0000FF",
        identifierOrAttribute: "#C53929",
        str: "#A31515",
        number: "#098658",
        punctuation: "#5F5F5F",
        type: "#267F99");

    private static readonly Palette LightXamlPalette = CreatePalette(
        plain: "#1E1E1E",
        comment: "#008000",
        keywordOrTag: "#800000",
        identifierOrAttribute: "#E50000",
        str: "#0000FF",
        number: "#098658",
        punctuation: "#5F5F5F",
        type: "#267F99");

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed",
        "float", "for", "foreach", "get", "goto", "if", "implicit", "in", "init", "int",
        "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
        "operator", "out", "override", "params", "partial", "private", "protected", "public",
        "readonly", "record", "ref", "required", "return", "sbyte", "sealed", "set", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var",
        "virtual", "void", "volatile", "while", "with", "yield",
    };

    /// <summary>Trims outer blank lines and unindents shared leading whitespace.</summary>
    public static string NormalizeCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var normalized = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n');

        var start = 0;
        while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        var end = lines.Length - 1;
        while (end >= start && string.IsNullOrWhiteSpace(lines[end]))
        {
            end--;
        }

        if (start > end)
        {
            return string.Empty;
        }

        var minIndent = int.MaxValue;
        for (var i = start; i <= end; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var count = 0;
            while (count < line.Length && line[count] == ' ')
            {
                count++;
            }

            if (count < minIndent)
            {
                minIndent = count;
            }
        }

        if (minIndent == int.MaxValue)
        {
            minIndent = 0;
        }

        var result = new string[end - start + 1];
        for (var i = start; i <= end; i++)
        {
            var line = lines[i];
            result[i - start] = line.Length >= minIndent && !string.IsNullOrWhiteSpace(line)
                ? line[minIndent..]
                : string.Empty;
        }

        return string.Join('\n', result);
    }

    /// <summary>Highlights <paramref name="code"/> into a <see cref="FlowDocument"/> using theme-appropriate colors.</summary>
    public static FlowDocument Highlight(string code, CodeLanguage language, WorkbenchThemeVariant theme)
    {
        var normalized = NormalizeCode(code);
        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            PageWidth = 2600,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12,
            LineHeight = 18,
        };

        var paragraph = new Paragraph { Margin = new Thickness(0) };
        if (normalized.Length > 0)
        {
            var palette = language == CodeLanguage.Xaml
                ? (theme == WorkbenchThemeVariant.Light ? LightXamlPalette : DarkPalette)
                : (theme == WorkbenchThemeVariant.Light ? LightPalette : DarkPalette);

            if (language == CodeLanguage.Xaml)
            {
                TokenizeXaml(normalized, palette, paragraph);
            }
            else
            {
                TokenizeCSharp(normalized, palette, paragraph);
            }
        }

        document.Blocks.Add(paragraph);
        return document;
    }

    private static void TokenizeXaml(string text, Palette p, Paragraph paragraph)
    {
        var i = 0;
        var len = text.Length;

        while (i < len)
        {
            if (text.AsSpan(i).StartsWith("<!--", StringComparison.Ordinal))
            {
                var closeIdx = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                var end = closeIdx < 0 ? len : closeIdx + 3;
                Emit(paragraph, text[i..end], p.Comment);
                i = end;
                continue;
            }

            if (text[i] == '<')
            {
                var tagPrefix = i + 1 < len && text[i + 1] == '/' ? "</" : "<";
                Emit(paragraph, tagPrefix, p.Punctuation);
                i += tagPrefix.Length;

                // Element name
                var nameStart = i;
                while (i < len && !char.IsWhiteSpace(text[i]) && text[i] is not '>' and not '/')
                {
                    i++;
                }

                if (i > nameStart)
                {
                    Emit(paragraph, text[nameStart..i], p.KeywordOrTag);
                }

                // Attributes until '>' or '/>'
                while (i < len)
                {
                    if (char.IsWhiteSpace(text[i]))
                    {
                        var wsStart = i;
                        while (i < len && char.IsWhiteSpace(text[i]))
                        {
                            i++;
                        }

                        Emit(paragraph, text[wsStart..i], p.Plain);
                        continue;
                    }

                    if (text.AsSpan(i).StartsWith("/>", StringComparison.Ordinal))
                    {
                        Emit(paragraph, "/>", p.Punctuation);
                        i += 2;
                        break;
                    }

                    if (text[i] == '>')
                    {
                        Emit(paragraph, ">", p.Punctuation);
                        i++;
                        break;
                    }

                    if (text[i] == '=')
                    {
                        Emit(paragraph, "=", p.Punctuation);
                        i++;
                        continue;
                    }

                    if (text[i] is '"' or '\'')
                    {
                        var quote = text[i];
                        var strStart = i++;
                        while (i < len && text[i] != quote)
                        {
                            i++;
                        }

                        if (i < len)
                        {
                            i++;
                        }

                        Emit(paragraph, text[strStart..i], p.String);
                        continue;
                    }

                    var attrStart = i;
                    while (i < len && !char.IsWhiteSpace(text[i]) && text[i] is not '=' and not '>' and not '/' and not '"' and not '\'')
                    {
                        i++;
                    }

                    if (i > attrStart)
                    {
                        Emit(paragraph, text[attrStart..i], p.IdentifierOrAttribute);
                    }
                    else
                    {
                        Emit(paragraph, text[i..(i + 1)], p.Plain);
                        i++;
                    }
                }

                continue;
            }

            var contentStart = i;
            while (i < len && text[i] != '<')
            {
                i++;
            }

            Emit(paragraph, text[contentStart..i], p.Plain);
        }
    }

    private static void TokenizeCSharp(string text, Palette p, Paragraph paragraph)
    {
        var i = 0;
        var len = text.Length;

        while (i < len)
        {
            if (text.AsSpan(i).StartsWith("//", StringComparison.Ordinal))
            {
                var nl = text.IndexOf('\n', i + 2);
                var end = nl < 0 ? len : nl;
                Emit(paragraph, text[i..end], p.Comment);
                i = end;
                continue;
            }

            if (text.AsSpan(i).StartsWith("/*", StringComparison.Ordinal))
            {
                var closeIdx = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                var end = closeIdx < 0 ? len : closeIdx + 2;
                Emit(paragraph, text[i..end], p.Comment);
                i = end;
                continue;
            }

            // Verbatim / interpolated string or regular string
            if (text.AsSpan(i).StartsWith("@\"", StringComparison.Ordinal) ||
                text.AsSpan(i).StartsWith("$\"", StringComparison.Ordinal) ||
                text[i] == '"')
            {
                var isVerbatim = text[i] == '@';
                var start = i;
                i += text[i] == '"' ? 1 : 2;
                while (i < len)
                {
                    if (isVerbatim)
                    {
                        if (text[i] == '"')
                        {
                            if (i + 1 < len && text[i + 1] == '"')
                            {
                                i += 2;
                                continue;
                            }

                            i++;
                            break;
                        }
                    }
                    else
                    {
                        if (text[i] == '\\' && i + 1 < len)
                        {
                            i += 2;
                            continue;
                        }

                        if (text[i] == '"' || text[i] == '\n')
                        {
                            if (text[i] == '"')
                            {
                                i++;
                            }

                            break;
                        }
                    }

                    i++;
                }

                Emit(paragraph, text[start..i], p.String);
                continue;
            }

            if (text[i] == '\'')
            {
                var start = i++;
                while (i < len)
                {
                    if (text[i] == '\\' && i + 1 < len)
                    {
                        i += 2;
                        continue;
                    }

                    if (text[i] == '\'' || text[i] == '\n')
                    {
                        if (text[i] == '\'')
                        {
                            i++;
                        }

                        break;
                    }

                    i++;
                }

                Emit(paragraph, text[start..i], p.String);
                continue;
            }

            if (char.IsDigit(text[i]))
            {
                var start = i;
                while (i < len && (char.IsLetterOrDigit(text[i]) || text[i] is '.' or '_'))
                {
                    i++;
                }

                Emit(paragraph, text[start..i], p.Number);
                continue;
            }

            if (char.IsLetter(text[i]) || text[i] == '_')
            {
                var start = i;
                while (i < len && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                var ident = text[start..i];
                var brush = CSharpKeywords.Contains(ident)
                    ? p.KeywordOrTag
                    : (char.IsUpper(ident[0]) && ident.Any(char.IsLower) ? p.Type : p.Plain);
                Emit(paragraph, ident, brush);
                continue;
            }

            if (char.IsWhiteSpace(text[i]))
            {
                var start = i;
                while (i < len && char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                Emit(paragraph, text[start..i], p.Plain);
                continue;
            }

            var punctStart = i;
            while (i < len && !char.IsLetterOrDigit(text[i]) && !char.IsWhiteSpace(text[i]) &&
                   text[i] is not '_' and not '"' and not '\'' and not '@' and not '$' and not '/')
            {
                i++;
            }

            if (i == punctStart)
            {
                i++;
            }

            Emit(paragraph, text[punctStart..i], p.Punctuation);
        }
    }

    private static void Emit(Paragraph paragraph, string segment, Brush brush)
    {
        if (segment.Length == 0)
        {
            return;
        }

        if (paragraph.Inlines.LastInline is Run lastRun && ReferenceEquals(lastRun.Foreground, brush))
        {
            lastRun.Text += segment;
            return;
        }

        paragraph.Inlines.Add(new Run(segment) { Foreground = brush });
    }

    private static Palette CreatePalette(
        string plain,
        string comment,
        string keywordOrTag,
        string identifierOrAttribute,
        string str,
        string number,
        string punctuation,
        string type) => new(
        FreezeBrush(plain),
        FreezeBrush(comment),
        FreezeBrush(keywordOrTag),
        FreezeBrush(identifierOrAttribute),
        FreezeBrush(str),
        FreezeBrush(number),
        FreezeBrush(punctuation),
        FreezeBrush(type));

    private static SolidColorBrush FreezeBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
