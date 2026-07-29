// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayNaming"/>.
/// </summary>
public class ScreenplayNaming : IScreenplayNaming
{
    /// <summary>
    /// The character the Screenplay printer cannot escape inside a string literal.
    /// </summary>
    public const char Quote = '"';

    /// <summary>
    /// The name used for a property whose own name yields nothing usable.
    /// </summary>
    public const string DefaultPropertyName = "value";

    /// <inheritdoc/>
    public string ToPropertyName(string name)
    {
        var identifier = Sanitize(name);
        if (identifier.Length == 0)
        {
            return DefaultPropertyName;
        }

        var builder = new StringBuilder(identifier);
        for (var index = 0; index < builder.Length; index++)
        {
            if (!char.IsUpper(builder[index]))
            {
                break;
            }

            // Preserve the casing of an acronym's final character when it starts a new word - "ISBNValue" becomes "isbnValue".
            if (index > 0 && index + 1 < builder.Length && !char.IsUpper(builder[index + 1]))
            {
                break;
            }

            builder[index] = char.ToLowerInvariant(builder[index]);
        }

        var result = builder.ToString();

        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }

    /// <inheritdoc/>
    public string ToPropertyPath(string path) =>
        string.IsNullOrWhiteSpace(path)
            ? DefaultPropertyName
            : string.Join('.', path.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(ToPropertyName));

    /// <inheritdoc/>
    public string ToDeclarationName(string name)
    {
        var identifier = Sanitize(name);

        return identifier.Length == 0 || char.IsDigit(identifier[0]) ? $"_{identifier}" : identifier;
    }

    /// <inheritdoc/>
    public string? ToStringLiteral(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = OnOneLine(value.Replace(Quote, '\''));

        return sanitized.Length == 0 ? null : sanitized;
    }

    /// <inheritdoc/>
    public string? ToFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var sanitized = OnOneLine(path.Replace('\\', '/').Replace(Quote, '\''));

        return sanitized.Length == 0 ? null : sanitized;
    }

    /// <summary>
    /// Reduces a value to the single line every Screenplay construct is written on.
    /// </summary>
    /// <param name="value">The value to reduce.</param>
    /// <returns>The value on one line.</returns>
    /// <remarks>
    /// The language is line based and the printer never escapes, so a line break inside a string literal ends the
    /// construct halfway through and everything after it is read as a new declaration - a document that does not
    /// compile. Every run of whitespace, control character included, therefore becomes a single space.
    /// </remarks>
    static string OnOneLine(string value)
    {
        var builder = new StringBuilder(value.Length);
        var separated = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                separated = builder.Length > 0;
                continue;
            }

            if (separated)
            {
                builder.Append(' ');
                separated = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Strips everything that is not a valid identifier character, including generic type arity suffixes, and joins
    /// separator-carrying names into a readable identifier.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    /// <remarks>
    /// A runtime name is idiomatically written with separators - a Chronicle constraint named <c>unique-timesheet-start</c>
    /// is the common case. A Screenplay identifier cannot hold a separator, so the segments a separator marks are
    /// PascalCased and joined rather than run together, which is the difference between <c>UniqueTimesheetStart</c> and
    /// an unreadable <c>uniquetimesheetstart</c>. A name that carries no separator is left exactly as it was, so a
    /// name already shaped like an identifier - and the acronym casing another step relies on - is untouched.
    /// </remarks>
    static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var backTick = name.IndexOf('`', StringComparison.Ordinal);
        var candidate = backTick > 0 ? name[..backTick] : name;
        var segments = Segments(candidate);

        if (segments.Count <= 1)
        {
            return StripToIdentifier(candidate).Normalize(NormalizationForm.FormC);
        }

        var builder = new StringBuilder(candidate.Length);
        foreach (var segment in segments)
        {
            builder.Append(char.ToUpperInvariant(segment[0]));
            builder.Append(segment, 1, segment.Length - 1);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Splits a name into the runs of letters and digits the separators between them mark as words.
    /// </summary>
    /// <param name="candidate">The name to split.</param>
    /// <returns>The segments, in order.</returns>
    static List<string> Segments(string candidate)
    {
        var segments = new List<string>();
        var builder = new StringBuilder(candidate.Length);

        foreach (var character in candidate)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (builder.Length > 0)
            {
                segments.Add(builder.ToString());
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            segments.Add(builder.ToString());
        }

        return segments;
    }

    /// <summary>
    /// Keeps only the characters a single-segment name is allowed to carry, preserving the historical shape of a name
    /// that carries no separator to bridge.
    /// </summary>
    /// <param name="candidate">The name to strip.</param>
    /// <returns>The stripped name.</returns>
    static string StripToIdentifier(string candidate)
    {
        var builder = new StringBuilder(candidate.Length);

        foreach (var character in candidate)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
