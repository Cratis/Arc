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
    public static IReadOnlyList<ScreenImport> Statements(string? text) => Statements(TsxSource.Scan(text ?? string.Empty));

    /// <summary>
    /// Gets every name a file imports, from any module, under the name the file calls it by.
    /// </summary>
    /// <param name="text">The text of the file, or <see langword="null"/> when it could not be read.</param>
    /// <returns>The bindings, keyed by the name the file calls each import by.</returns>
    /// <remarks>
    /// A component is written under the name the file gives it, so telling a Cratis Components <c>DataPage</c> from
    /// anything else of the same name - and a renamed query proxy from the query it is - takes the local name, the
    /// exported name and the module together. Only named imports are read, for the same reason <see cref="Statements(string)"/>
    /// reads only those.
    /// </remarks>
    public static IReadOnlyDictionary<string, ScreenImportBinding> Bindings(string? text) => Bindings(TsxSource.Scan(text ?? string.Empty));

    /// <summary>
    /// Reads real import statements from a scanned module.
    /// </summary>
    /// <param name="source">The scanned module.</param>
    /// <returns>The relative named imports.</returns>
    internal static IReadOnlyList<ScreenImport> Statements(TsxSource source)
    {
        var imports = new List<ScreenImport>();
        var seen = new HashSet<ScreenImport>();
        foreach (var statement in ImportStatements(source))
        {
            var module = statement.Groups["module"].Value;
            if (!IsRelative(statement) || IsTypeOnly(statement))
            {
                continue;
            }

            imports.AddRange(NamedIn(statement).Select(_ => new ScreenImport(_, module)).Where(seen.Add));
        }

        return imports;
    }

    /// <summary>
    /// Reads real named import bindings from a scanned module.
    /// </summary>
    /// <param name="source">The scanned module.</param>
    /// <returns>The bindings by local name.</returns>
    internal static IReadOnlyDictionary<string, ScreenImportBinding> Bindings(TsxSource source)
    {
        var bindings = new Dictionary<string, ScreenImportBinding>(StringComparer.Ordinal);
        foreach (var statement in ImportStatements(source))
        {
            if (IsTypeOnly(statement))
            {
                continue;
            }

            foreach (var (name, local) in LocalNamesIn(statement))
            {
                bindings.TryAdd(local, new(local, name, statement.Groups["module"].Value));
            }
        }

        return bindings;
    }

    static IEnumerable<Match> ImportStatements(TsxSource source) =>
        StatementRegex().Matches(source.Code).Cast<Match>().Where(_ => source.IsCode(_.Index + _.Value.IndexOf("import", StringComparison.Ordinal)));

    /// <summary>
    /// Determines whether a module specifier names a file sitting alongside the one importing it.
    /// </summary>
    /// <param name="statement">The import statement.</param>
    /// <returns>True when the specifier is relative.</returns>
    static bool IsRelative(Match statement)
    {
        var module = statement.Groups["module"].Value;

        return module.StartsWith("./", StringComparison.Ordinal) || module.StartsWith("../", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether an import clause brings in types rather than values.
    /// </summary>
    /// <param name="statement">The import statement to check.</param>
    /// <returns>True when the whole clause is type only.</returns>
    /// <remarks>
    /// A type only import is erased before anything runs, so it says the file mentions a shape rather than that it
    /// reads through it. Binding a screen to a query it never calls would be a plain untruth.
    /// </remarks>
    static bool IsTypeOnly(Match statement) => TypeOnlyRegex().IsMatch(statement.Groups["clause"].Value);

    /// <summary>
    /// Gets the exported names an import clause brings in.
    /// </summary>
    /// <param name="statement">The import statement to read.</param>
    /// <returns>The names.</returns>
    static IEnumerable<string> NamedIn(Match statement) => LocalNamesIn(statement).Select(_ => _.Name);

    /// <summary>
    /// Gets the exported names an import clause brings in, each with the name the importing file calls it by.
    /// </summary>
    /// <param name="statement">The import statement to read.</param>
    /// <returns>The exported and local names.</returns>
    static IEnumerable<(string Name, string Local)> LocalNamesIn(Match statement)
    {
        var clause = statement.Groups["clause"].Value;
        var open = clause.IndexOf('{', StringComparison.Ordinal);
        var close = clause.LastIndexOf('}');
        if (open < 0 || close <= open)
        {
            yield break;
        }

        foreach (var specifier in clause[(open + 1)..close].Split(','))
        {
            if (NamesIn(specifier) is { } names)
            {
                yield return names;
            }
        }
    }

    /// <summary>
    /// Gets the name a module exports a specifier under and the name the importing file renames it to.
    /// </summary>
    /// <param name="specifier">The specifier to read.</param>
    /// <returns>The exported and local names, or <see langword="null"/> when the specifier is not one.</returns>
    static (string Name, string Local)? NamesIn(string specifier)
    {
        var words = specifier.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        (string Name, string Local)? names = words switch
        {
            ["type", ..] => null,
            [var only] => (only, only),
            [var name, "as", var local] => (name, local),
            _ => null
        };

        return names is { } read && IdentifierRegex().IsMatch(read.Name) && IdentifierRegex().IsMatch(read.Local) ? read : null;
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
