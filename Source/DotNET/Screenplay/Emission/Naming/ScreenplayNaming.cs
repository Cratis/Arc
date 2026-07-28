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

        var sanitized = value.Replace(Quote, '\'').Trim();

        return sanitized.Length == 0 ? null : sanitized;
    }

    /// <inheritdoc/>
    public string? ToFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var sanitized = path.Replace('\\', '/').Replace(Quote, '\'').Trim();

        return sanitized.Length == 0 ? null : sanitized;
    }

    /// <summary>
    /// Strips everything that is not a valid identifier character, including generic type arity suffixes.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var backTick = name.IndexOf('`', StringComparison.Ordinal);
        var candidate = backTick > 0 ? name[..backTick] : name;
        var builder = new StringBuilder(candidate.Length);

        foreach (var character in candidate)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
