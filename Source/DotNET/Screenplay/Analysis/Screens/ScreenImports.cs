// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads the names a user interface file imports from the files sitting alongside it.
/// </summary>
/// <remarks>
/// This is the whole of what is read out of TypeScript, and it is read because an import statement is the one
/// construct in the language whose meaning survives being looked at in isolation - a name, and where it came from.
/// Nothing here interprets what the component does with the name; that is JSX, and a guess at it would be a
/// falsehood stated confidently. The name is only ever handed to the model to be checked against what the slice
/// really declares, so an import naming something the slice does not declare says nothing and is dropped.
/// </remarks>
public static partial class ScreenImports
{
    /// <summary>
    /// Gets the names a file imports from a module sitting alongside it.
    /// </summary>
    /// <param name="text">The text of the file, or <see langword="null"/> when it could not be read.</param>
    /// <returns>The imported names, as the module exports them rather than as the file renames them.</returns>
    /// <remarks>
    /// Only named imports of a relative module are read. A generated proxy is a named export written next to the
    /// slice, so that is the shape every real binding has, and the shapes that are left out - a default import, a
    /// namespace import, an import of a package - cannot be tied to an exported name with any certainty.
    /// </remarks>
    public static IReadOnlyCollection<string> In(string? text)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return names;
        }

        foreach (var statement in StatementRegex().Matches(text).Cast<Match>())
        {
            var clause = statement.Groups["clause"].Value;
            if (!IsRelative(statement.Groups["module"].Value) || IsTypeOnly(clause))
            {
                continue;
            }

            foreach (var name in NamedIn(clause))
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>
    /// Determines whether a module specifier names a file sitting alongside the one importing it.
    /// </summary>
    /// <param name="module">The module specifier.</param>
    /// <returns>True when the specifier is relative.</returns>
    static bool IsRelative(string module) =>
        module.StartsWith("./", StringComparison.Ordinal) || module.StartsWith("../", StringComparison.Ordinal);

    /// <summary>
    /// Determines whether an import clause brings in types rather than values.
    /// </summary>
    /// <param name="clause">The clause to check.</param>
    /// <returns>True when the whole clause is type only.</returns>
    /// <remarks>
    /// A type only import is erased before anything runs, so it says the file mentions a shape rather than that it
    /// reads through it. Binding a screen to a query it never calls would be a plain untruth.
    /// </remarks>
    static bool IsTypeOnly(string clause) => TypeOnlyRegex().IsMatch(clause);

    /// <summary>
    /// Gets the exported names an import clause brings in.
    /// </summary>
    /// <param name="clause">The clause to read.</param>
    /// <returns>The names.</returns>
    static IEnumerable<string> NamedIn(string clause)
    {
        var open = clause.IndexOf('{', StringComparison.Ordinal);
        var close = clause.LastIndexOf('}');
        if (open < 0 || close <= open)
        {
            yield break;
        }

        foreach (var specifier in clause[(open + 1)..close].Split(','))
        {
            if (ExportedNameIn(specifier) is { } name)
            {
                yield return name;
            }
        }
    }

    /// <summary>
    /// Gets the name a module exports a specifier under, seeing past whatever the importing file renames it to.
    /// </summary>
    /// <param name="specifier">The specifier to read.</param>
    /// <returns>The exported name, or <see langword="null"/> when the specifier is not one.</returns>
    static string? ExportedNameIn(string specifier)
    {
        var words = specifier.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var exported = words switch
        {
            ["type", ..] => null,
            [var only] => only,
            [var name, "as", _] => name,
            _ => null
        };

        return exported is not null && IdentifierRegex().IsMatch(exported) ? exported : null;
    }

    /// <summary>
    /// Gets the pattern an import statement naming a module has to match.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(
        """^[ \t]*import\b(?<clause>[^;'"]*?)\bfrom\s*(?<quote>['"])(?<module>[^'"]*)\k<quote>""",
        RegexOptions.Multiline,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex StatementRegex();

    /// <summary>
    /// Gets the pattern a type only import clause has to match.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^\s*type\b", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TypeOnlyRegex();

    /// <summary>
    /// Gets the pattern an imported name has to match.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^[A-Za-z_$][A-Za-z0-9_$]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdentifierRegex();
}
