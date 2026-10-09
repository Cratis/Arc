// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents the value a JSX attribute is given.
/// </summary>
/// <param name="Written">The value exactly as written - the text of a string, or the expression between braces.</param>
/// <param name="Literal">The text the value is when it is written as text rather than computed, otherwise null.</param>
public partial record JsxValue(string Written, string? Literal)
{
    /// <summary>
    /// Gets the name the value is when it is nothing but a name - <c>query={AllAuthors}</c> - otherwise null.
    /// </summary>
    public string? Identifier => Literal is null && IdentifierRegex().IsMatch(Written) ? Written : null;

    /// <summary>
    /// Gets the value of an attribute written between braces.
    /// </summary>
    /// <param name="expression">The expression between the braces.</param>
    /// <returns>The <see cref="JsxValue"/>.</returns>
    /// <remarks>
    /// A string or a template without interpolation is text as surely as a quoted attribute is. Anything with an
    /// escape in it is left as an expression rather than unescaped by a reader that does not know the language.
    /// </remarks>
    public static JsxValue Expression(string expression)
    {
        var trimmed = expression.Trim();
        var isText = trimmed.Length >= 2 &&
            trimmed[0] is '"' or '\'' or '`' &&
            trimmed[^1] == trimmed[0] &&
            !trimmed[1..^1].Contains(trimmed[0]) &&
            !trimmed.Contains('\\') &&
            !trimmed.Contains("${", StringComparison.Ordinal);

        return new(trimmed, isText ? trimmed[1..^1] : null);
    }

    /// <summary>
    /// Gets the value of an attribute written as a quoted string.
    /// </summary>
    /// <param name="text">The text between the quotes.</param>
    /// <returns>The <see cref="JsxValue"/>.</returns>
    /// <remarks>
    /// JSX decodes character references in a quoted attribute, so text holding one is not the text it shows and
    /// is left as written rather than decoded by a guess.
    /// </remarks>
    public static JsxValue Quoted(string text) => new(text, text.Contains('&') ? null : text);

    [GeneratedRegex(@"^[A-Za-z_$][A-Za-z0-9_$]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdentifierRegex();
}
