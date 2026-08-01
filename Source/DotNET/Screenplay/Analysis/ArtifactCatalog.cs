// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Holds every type the compilation declares, in a deterministic order and grouped by namespace.
/// </summary>
/// <remarks>
/// Only the types the compilation itself declares are catalogued. Artifacts living in a referenced package have
/// metadata but no body, so they are read on demand from where they are referenced rather than discovered here.
/// </remarks>
public class ArtifactCatalog
{
    readonly List<INamedTypeSymbol> _types;

    ArtifactCatalog(List<INamedTypeSymbol> types) => _types = types;

    /// <summary>
    /// Gets every declared type, ordered by fully qualified name.
    /// </summary>
    public IReadOnlyList<INamedTypeSymbol> Types => _types;

    /// <summary>
    /// Gets the namespaces holding at least one type, ordered.
    /// </summary>
    public IEnumerable<string> Namespaces => _types
        .Select(_ => _.Namespace())
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal);

    /// <summary>
    /// Catalogues everything a compilation declares.
    /// </summary>
    /// <param name="compilation">The compilation to catalogue.</param>
    /// <returns>The <see cref="ArtifactCatalog"/>.</returns>
    public static ArtifactCatalog From(Compilation compilation)
    {
        var types = new List<INamedTypeSymbol>();
        Collect(compilation.Assembly.GlobalNamespace, types);

        return new([.. types.OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal)]);
    }

    /// <summary>
    /// Gets the types declared within a namespace.
    /// </summary>
    /// <param name="namespace">The namespace to read.</param>
    /// <returns>The types, ordered by name.</returns>
    public IEnumerable<INamedTypeSymbol> In(string @namespace) =>
        _types.Where(_ => string.Equals(_.Namespace(), @namespace, StringComparison.Ordinal));

    /// <summary>
    /// Collects every type declared within a namespace, including nested types.
    /// </summary>
    /// <param name="namespace">The namespace to walk.</param>
    /// <param name="types">The types collected so far.</param>
    static void Collect(INamespaceSymbol @namespace, List<INamedTypeSymbol> types)
    {
        foreach (var member in @namespace.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol nested:
                    Collect(nested, types);
                    break;
                case INamedTypeSymbol type:
                    Collect(type, types);
                    break;
            }
        }
    }

    /// <summary>
    /// Collects a type and everything nested within it.
    /// </summary>
    /// <param name="type">The type to collect.</param>
    /// <param name="types">The types collected so far.</param>
    static void Collect(INamedTypeSymbol type, List<INamedTypeSymbol> types)
    {
        types.Add(type);

        foreach (var nested in type.GetTypeMembers())
        {
            Collect(nested, types);
        }
    }
}
