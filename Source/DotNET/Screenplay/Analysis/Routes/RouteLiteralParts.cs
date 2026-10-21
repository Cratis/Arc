// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Splits literal containers at their own boundaries, skipping strings, expressions and complete JSX elements.
/// </summary>
internal static class RouteLiteralParts
{
    /// <summary>
    /// Reads the comma-separated members of a whole array or object literal.
    /// </summary>
    /// <param name="text">The expression.</param>
    /// <param name="opening">The required container opening.</param>
    /// <returns>The members, or null when the expression is not a whole literal.</returns>
    internal static IReadOnlyList<string>? Members(string text, char opening)
    {
        var code = TsxSource.Scan(text.Trim());
        if (code.Problem is not null || code.Code.Length < 2 || code.Code[0] != opening || EndOf(code, 0) != code.Code.Length)
        {
            return null;
        }

        return Split(code.Code[1..^1], ',');
    }

    /// <summary>
    /// Reads unambiguous literal object fields, excluding spreads and duplicate keys.
    /// </summary>
    /// <param name="text">The object expression.</param>
    /// <returns>The fields, or null when not entirely a literal object.</returns>
    internal static IReadOnlyDictionary<string, string>? Fields(string text)
    {
        if (Members(text, '{') is not { } members)
        {
            return null;
        }

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            var parts = Split(member, ':');
            var key = parts.Count == 2 ? JsxValue.Expression(parts[0]) : null;
            var name = key?.Identifier ?? key?.Literal;
            if (name is null || !fields.TryAdd(name, parts[1].Trim()))
            {
                return null;
            }
        }

        return fields;
    }

    /// <summary>
    /// Gets the exclusive end of a scanned literal or balanced bracket/parenthesis container.
    /// </summary>
    /// <param name="source">The scanned source.</param>
    /// <param name="start">The opening offset.</param>
    /// <returns>The end, or -1 when no boundary is known.</returns>
    internal static int EndOf(TsxSource source, int start)
    {
        if (source.CloseOf(start) is var known && known >= 0)
        {
            return known;
        }

        if (source.Tags.FirstOrDefault(_ => _.Start == start) is { IsComplete: true } tag)
        {
            return tag.End;
        }

        return source.Code[start] switch
        {
            '[' => BalancedEnd(source, start, ']'),
            '(' => BalancedEnd(source, start, ')'),
            _ => -1
        };
    }

    static int BalancedEnd(TsxSource source, int start, char closing)
    {
        for (var index = start + 1; index < source.Code.Length; index++)
        {
            if (source.Code[index] == closing)
            {
                return index + 1;
            }

            if (EndOf(source, index) is var end && end > index)
            {
                index = end - 1;
            }
        }

        return -1;
    }

    static List<string> Split(string text, char separator)
    {
        var source = TsxSource.Scan(text);
        var parts = new List<string>();
        var start = 0;
        for (var index = 0; index < source.Code.Length; index++)
        {
            if (source.Code[index] == separator)
            {
                parts.Add(source.Code[start..index].Trim());
                start = index + 1;
            }
            else if (EndOf(source, index) is var end && end > index)
            {
                index = end - 1;
            }
        }

        if (!string.IsNullOrWhiteSpace(source.Code[start..]))
        {
            parts.Add(source.Code[start..].Trim());
        }

        return parts;
    }
}
