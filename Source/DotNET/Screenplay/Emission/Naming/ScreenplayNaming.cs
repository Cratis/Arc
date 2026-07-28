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
    /// Reduces a name to the identifier the Screenplay grammar accepts, including generic type arity suffixes.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    /// <remarks>
    /// An identifier is <c>[A-Za-z_]\w*</c>, so a name carrying separators has to be transformed - and a runtime name
    /// is routinely written with them, kebab case being idiomatic for a Chronicle constraint. Deleting them is the
    /// least readable answer available: <c>unique-timesheet-start</c> becomes one run-together word and the word
    /// boundaries the source stated are thrown away. Each separator is therefore read as the boundary it is and the
    /// words either side of it are joined in Pascal case, which is what the grammar accepts and what a reader would
    /// have written by hand. A name carrying no separator at all is left exactly as it is, because its casing is
    /// already whatever the application chose.
    /// </remarks>
    static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var backTick = name.IndexOf('`', StringComparison.Ordinal);
        var words = WordsIn(backTick > 0 ? name[..backTick] : name);

        var identifier = words.Count switch
        {
            0 => string.Empty,
            1 => words[0],
            _ => string.Concat(words.Select(Capitalized))
        };

        return identifier.Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Splits a name into the words the characters that cannot be written in an identifier separate.
    /// </summary>
    /// <param name="name">The name to split.</param>
    /// <returns>The words, in order, none of them empty.</returns>
    /// <remarks>
    /// An underscore separates words as surely as a hyphen does, and is treated as one even though the grammar would
    /// accept it, so that every way of writing a name apart comes out the same way.
    /// </remarks>
    static List<string> WordsIn(string name)
    {
        var words = new List<string>();
        var word = new StringBuilder(name.Length);

        foreach (var character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                word.Append(character);
                continue;
            }

            if (word.Length > 0)
            {
                words.Add(word.ToString());
                word.Clear();
            }
        }

        if (word.Length > 0)
        {
            words.Add(word.ToString());
        }

        return words;
    }

    /// <summary>
    /// Raises the first character of a word, leaving the rest of it as the application wrote it.
    /// </summary>
    /// <param name="word">The word to raise.</param>
    /// <returns>The raised word.</returns>
    static string Capitalized(string word) => $"{char.ToUpperInvariant(word[0])}{word[1..]}";
}
