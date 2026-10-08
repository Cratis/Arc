// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Finds the JSX elements written with a given tag, and reads their attributes and children.
/// </summary>
/// <remarks>
/// This is not a TypeScript parser and does not try to be one. It reads exactly one shape - an element written with
/// a known tag, whose attributes are quoted strings, expressions between braces or bare names - and anything else is
/// marked unreadable rather than read approximately. A reader that knows it has not understood something is what lets
/// the caller leave it out and say so, instead of writing something the component does not say.
/// </remarks>
public static partial class JsxElements
{
    /// <summary>
    /// Finds every element written with a tag, outermost first and in the order the text writes them.
    /// </summary>
    /// <param name="text">The text to look in, with comments already removed.</param>
    /// <param name="tag">The tag, exactly as the file writes it - <c>DataPage</c>, <c>DataPage.MenuItem</c>.</param>
    /// <returns>The elements. One nested in another of the same tag is part of the outer one's children.</returns>
    public static IEnumerable<JsxElement> Named(string text, string tag)
    {
        var position = 0;
        while (Opening(text, tag, position) is { } start)
        {
            var element = Read(text, tag, start, out var end);
            yield return element;
            position = end;
        }
    }

    static int? Opening(string text, string tag, int from)
    {
        var match = TagRegex(tag).Match(text, from);
        while (match.Success && match.Groups["closing"].Success)
        {
            match = match.NextMatch();
        }

        return match.Success ? match.Index : null;
    }

    static JsxElement Read(string text, string tag, int start, out int end)
    {
        var attributes = new Dictionary<string, JsxValue>(StringComparer.Ordinal);
        var opening = ReadOpening(text, start + 1 + tag.Length, attributes, out var readable, out var selfClosing);
        if (opening < 0)
        {
            end = start + 1 + tag.Length;
            return new(tag, start, new Dictionary<string, JsxValue>(), string.Empty, false);
        }

        if (selfClosing)
        {
            end = opening;
            return new(tag, start, attributes, string.Empty, readable);
        }

        if (Closing(text, tag, opening) is not { } closing)
        {
            end = opening;
            return new(tag, start, attributes, string.Empty, false);
        }

        end = closing.End;
        return new(tag, start, attributes, text[opening..closing.Start], readable);
    }

    static int ReadOpening(string text, int position, Dictionary<string, JsxValue> attributes, out bool readable, out bool selfClosing)
    {
        readable = true;
        selfClosing = false;

        // An element can carry type arguments - <Column<Issue> ... /> - which say nothing the document needs.
        if (position < text.Length && text[position] == '<')
        {
            position = SkipTypeArguments(text, position);
            if (position < 0)
            {
                return -1;
            }
        }

        while (true)
        {
            position = SkipWhitespace(text, position);
            if (position >= text.Length)
            {
                return -1;
            }

            if (text[position] == '>')
            {
                return position + 1;
            }

            if (text[position] == '/' && position + 1 < text.Length && text[position + 1] == '>')
            {
                selfClosing = true;
                return position + 2;
            }

            if (text[position] == '{')
            {
                var spreadEnd = Braced(text, position);
                if (spreadEnd < 0 || !text[(position + 1)..(spreadEnd - 1)].TrimStart().StartsWith("...", StringComparison.Ordinal))
                {
                    return -1;
                }

                readable = false;
                position = spreadEnd;
                continue;
            }

            var name = AttributeNameRegex().Match(text, position);
            if (!name.Success || name.Index != position)
            {
                return -1;
            }

            position = SkipWhitespace(text, position + name.Length);
            if (position >= text.Length || text[position] != '=')
            {
                attributes[name.Value] = new("true", null);
                continue;
            }

            position = SkipWhitespace(text, position + 1);
            if (ReadValue(text, position, out var value) is not { } next)
            {
                return -1;
            }

            attributes[name.Value] = value;
            position = next;
        }
    }

    static int? ReadValue(string text, int position, out JsxValue value)
    {
        value = new(string.Empty, null);
        if (position >= text.Length)
        {
            return null;
        }

        var quote = text[position];
        if (quote is '"' or '\'')
        {
            var close = text.IndexOf(quote, position + 1);
            if (close < 0)
            {
                return null;
            }

            value = JsxValue.Quoted(text[(position + 1)..close]);
            return close + 1;
        }

        if (quote != '{' || Braced(text, position) is not (> 0 and var end))
        {
            return null;
        }

        value = JsxValue.Expression(text[(position + 1)..(end - 1)]);
        return end;
    }

    static (int Start, int End)? Closing(string text, string tag, int from)
    {
        var depth = 1;
        for (var match = TagRegex(tag).Match(text, from); match.Success; match = match.NextMatch())
        {
            if (match.Groups["closing"].Success)
            {
                var close = text.IndexOf('>', match.Index);
                if (close < 0)
                {
                    return null;
                }

                if (--depth == 0)
                {
                    return (match.Index, close + 1);
                }

                continue;
            }

            var nested = ReadOpening(text, match.Index + 1 + tag.Length, [], out _, out var selfClosing);
            if (nested < 0)
            {
                return null;
            }

            depth += selfClosing ? 0 : 1;
        }

        return null;
    }

    static int SkipTypeArguments(string text, int open)
    {
        var depth = 0;
        for (var position = open; position < text.Length; position++)
        {
            switch (text[position])
            {
                case '<':
                    depth++;
                    break;
                case '>' when position > 0 && text[position - 1] == '=':
                    break;
                case '>':
                    if (--depth == 0)
                    {
                        return position + 1;
                    }

                    break;
                case '"' or '\'' or '`':
                    position = EndOfString(text, position);
                    if (position < 0)
                    {
                        return -1;
                    }

                    break;
                case '{' or '}' or ';':
                    return -1;
            }
        }

        return -1;
    }

    static int Braced(string text, int open)
    {
        var depth = 0;
        for (var position = open; position < text.Length; position++)
        {
            switch (text[position])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return position + 1;
                    }

                    break;
                case '"' or '\'' or '`':
                    position = EndOfString(text, position);
                    if (position < 0)
                    {
                        return -1;
                    }

                    break;
            }
        }

        return -1;
    }

    static int EndOfString(string text, int open)
    {
        for (var position = open + 1; position < text.Length; position++)
        {
            if (text[position] == '\\')
            {
                position++;
            }
            else if (text[position] == text[open])
            {
                return position;
            }
        }

        return -1;
    }

    static int SkipWhitespace(string text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    static Regex TagRegex(string tag) => new($@"<(?<closing>/)?\s*{Regex.Escape(tag)}(?=[\s/>]|<)", RegexOptions.None, TimeSpan.FromSeconds(1));

    [GeneratedRegex(@"[A-Za-z_$][\w$\-:]*", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex AttributeNameRegex();
}
