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
    public static IReadOnlyCollection<string> In(string? text) =>
        new HashSet<string>(Statements(text).Select(_ => _.Name), StringComparer.Ordinal);

    /// <summary>
    /// Gets the names a file imports from a module sitting alongside it, together with the module each came from.
    /// </summary>
    /// <param name="text">The text of the file, or <see langword="null"/> when it could not be read.</param>
    /// <returns>The imports, in the order the file writes them.</returns>
    /// <remarks>
    /// Source order is kept rather than sorted, because it is the order the file was written in and nothing else is
    /// any more meaningful - and keeping it is what makes the same file always read the same way.
    /// </remarks>
    public static IReadOnlyList<ScreenImport> Statements(string? text)
    {
        var imports = new List<ScreenImport>();
        var seen = new HashSet<ScreenImport>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return imports;
        }

        foreach (var statement in StatementRegex().Matches(WithoutComments(text)).Cast<Match>())
        {
            var clause = statement.Groups["clause"].Value;
            var module = statement.Groups["module"].Value;
            if (!IsRelative(module) || IsTypeOnly(clause))
            {
                continue;
            }

            imports.AddRange(NamedIn(clause).Select(_ => new ScreenImport(_, module)).Where(seen.Add));
        }

        return imports;
    }

    /// <summary>
    /// Removes everything a comment holds, leaving the lines around it where they were.
    /// </summary>
    /// <param name="text">The text of the file.</param>
    /// <returns>The text with nothing commented out left in it.</returns>
    /// <remarks>
    /// An import that has been commented out is an import the file does not make, and binding a screen to a query it
    /// no longer calls is a plain untruth of exactly the kind this reader exists to avoid. The line breaks a comment
    /// spans are kept, because a statement is recognized by starting a line and joining it to the line before would
    /// hide a real import rather than a commented one.
    /// </remarks>
    static string WithoutComments(string text) =>
        CommentRegex().Replace(text, match => new string('\n', match.Value.Count(_ => _ == '\n')));

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
    /// Gets the pattern a comment has to match.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"/\*[\s\S]*?\*/|//[^\r\n]*", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommentRegex();

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
