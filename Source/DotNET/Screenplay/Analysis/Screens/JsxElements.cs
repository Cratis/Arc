// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Finds JSX elements using scanned boundaries, and reads their attributes and children.
/// </summary>
public static partial class JsxElements
{
    /// <summary>
    /// Finds every outermost element with a tag, in source order.
    /// </summary>
    /// <param name="text">The JSX markup to look in.</param>
    /// <param name="tag">The tag exactly as written.</param>
    /// <returns>The elements, including unreadable ones.</returns>
    public static IEnumerable<JsxElement> Named(string text, string tag) => Named(TsxSource.ScanMarkup(text), tag);

    /// <summary>
    /// Reads outermost named elements from a scanned module.
    /// </summary>
    /// <param name="source">The scanned module.</param>
    /// <param name="tag">The tag to find.</param>
    /// <returns>The elements in source order.</returns>
    internal static IEnumerable<JsxElement> Named(TsxSource source, string tag) => Named(source, tag, 0, source.Code.Length);

    /// <summary>
    /// Reads named descendants without scanning the children again.
    /// </summary>
    /// <param name="parent">The containing element.</param>
    /// <param name="tag">The tag to find.</param>
    /// <returns>The outermost named descendants.</returns>
    internal static IEnumerable<JsxElement> Named(JsxElement parent, string tag) => parent.Source is { } source
        ? Named(source, tag, parent.ChildrenStart, parent.ChildrenEnd)
        : Named(parent.Children, tag);

    static IEnumerable<JsxElement> Named(TsxSource source, string name, int start, int end)
    {
        var next = start;
        foreach (var tag in source.Tags.Where(_ => _.Name == name))
        {
            if (tag.Start < next || tag.Start >= end)
            {
                continue;
            }

            yield return Read(source, tag);
            next = Math.Max(tag.Start + 1, tag.End);
        }
    }

    static JsxElement Read(TsxSource source, JsxTag tag)
    {
        var code = source.Code;
        var attributes = new Dictionary<string, JsxValue>(StringComparer.Ordinal);
        var readable = tag.IsComplete && ReadAttributes(source, code, tag, attributes);
        var childrenEnd = tag.SelfClosing ? tag.OpeningEnd : tag.ChildrenEnd;
        var children = tag.IsComplete ? code[tag.OpeningEnd..childrenEnd] : string.Empty;

        return new(tag.Name, tag.Start, attributes, children, readable)
        {
            Source = source,
            End = tag.End,
            ChildrenStart = tag.OpeningEnd,
            ChildrenEnd = childrenEnd
        };
    }

    static bool ReadAttributes(TsxSource source, string code, JsxTag tag, Dictionary<string, JsxValue> attributes)
    {
        var position = tag.AttributesStart;
        if (At(code, position, '<'))
        {
            position = source.CloseOf(position);
        }

        var readable = true;
        while (position < tag.OpeningEnd)
        {
            position = SkipWhitespace(code, position);
            if (AtEnd(code, position))
            {
                return readable;
            }

            if (!ReadAttribute(source, code, attributes, ref position, ref readable))
            {
                return false;
            }
        }

        return false;
    }

    static bool ReadAttribute(TsxSource source, string code, Dictionary<string, JsxValue> attributes, ref int position, ref bool readable)
    {
        if (At(code, position, '{'))
        {
            return ReadSpread(source, code, ref position, ref readable);
        }

        var name = AttributeNameRegex().Match(code, position);
        if (!name.Success || name.Index != position)
        {
            return false;
        }

        position = SkipWhitespace(code, position + name.Length);
        if (!At(code, position, '='))
        {
            attributes[name.Value] = new("true", null);
            return true;
        }

        position = SkipWhitespace(code, position + 1);
        if (ReadValue(source, code, position, out var value) is not { } next)
        {
            return false;
        }

        attributes[name.Value] = value;
        position = next;

        return true;
    }

    static bool ReadSpread(TsxSource source, string code, ref int position, ref bool readable)
    {
        var end = source.CloseOf(position);
        if (end < 0 || !code[(position + 1)..(end - 1)].TrimStart().StartsWith("...", StringComparison.Ordinal))
        {
            return false;
        }

        readable = false;
        position = end;

        return true;
    }

    static int? ReadValue(TsxSource source, string code, int position, out JsxValue value)
    {
        value = new(string.Empty, null);
        var end = source.CloseOf(position);
        if (end < 0)
        {
            return null;
        }

        var written = code[(position + 1)..(end - 1)];
        value = code[position] == '{' ? JsxValue.Expression(written) : JsxValue.Quoted(written);

        return end;
    }

    static bool At(string code, int position, char character) => position >= 0 && IsAt(code, position, character);

    static bool IsAt(string code, int position, char character) => position < code.Length && code[position] == character;

    static bool AtEnd(string code, int position) => At(code, position, '>') || At(code, position, '/');

    static int SkipWhitespace(string text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    [GeneratedRegex(@"[A-Za-z_$][\w$\-:]*", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex AttributeNameRegex();
}
