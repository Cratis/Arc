// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Resolves the names the queries of a slice are declared under.
/// </summary>
/// <remarks>
/// Two read models in one namespace declaring a query of the same name produce a document with two declarations
/// that cannot be told apart. The grammar tolerates it, which is worse than rejecting it - the document reads as if
/// one of them were a duplicate. Every colliding query is therefore qualified by the type that declared it, so both
/// survive and both are traceable back to source, and the qualification is reported rather than done silently.
/// <para>
/// Every colliding name is qualified rather than only the later ones, because "first wins" would make the name a
/// query is declared under depend on what else happens to be in the namespace.
/// </para>
/// </remarks>
public static class QueryNames
{
    /// <summary>
    /// Resolves the queries of a slice onto names that tell them apart.
    /// </summary>
    /// <param name="declared">The queries, together with the types that declared them.</param>
    /// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything ambiguous is reported to.</param>
    /// <param name="location">Where the slice lives, for use in diagnostics.</param>
    /// <returns>The queries, under names that are unique within the slice.</returns>
    public static IEnumerable<QueryModel> Resolve(
        IEnumerable<DeclaredQuery> declared,
        ScreenplayDiagnostics diagnostics,
        string location)
    {
        var all = declared as IReadOnlyCollection<DeclaredQuery> ?? [.. declared];
        var colliding = all
            .GroupBy(_ => _.Query.Name, StringComparer.Ordinal)
            .Where(_ => _.Count() > 1)
            .Select(_ => _.Key)
            .ToHashSet(StringComparer.Ordinal);

        var taken = new HashSet<string>(StringComparer.Ordinal);
        var resolved = new List<QueryModel>();

        foreach (var entry in all)
        {
            var name = colliding.Contains(entry.Query.Name) ? $"{entry.DeclaringName}{entry.Query.Name}" : entry.Query.Name;

            if (!taken.Add(name))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.AmbiguousQueryName,
                    $"'{entry.DeclaringName}' declares a second query that resolves to '{name}', which nothing can tell apart, so it was left out",
                    location);

                continue;
            }

            if (!string.Equals(name, entry.Query.Name, StringComparison.Ordinal))
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.AmbiguousQueryName,
                    $"More than one query in the slice is called '{entry.Query.Name}', so the one on '{entry.DeclaringName}' is declared as '{name}'",
                    location);
            }

            resolved.Add(entry.Query with { Name = name });
        }

        return resolved;
    }
}
