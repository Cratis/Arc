// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Holds the names of the Cypher parameters Arc adds to every query it runs through <see cref="IGraphSession"/>.
/// </summary>
public static class GraphParameters
{
    /// <summary>
    /// The name of the parameter holding the current namespace, used as <c>$namespace</c>.
    /// </summary>
    /// <remarks>
    /// In <see cref="GraphDatabaseMode.Single"/> mode, filter with <c>WHERE n._namespace = $namespace</c>. In
    /// <see cref="GraphDatabaseMode.PerNamespace"/> mode the database is the filter, and the parameter is available but unused.
    /// </remarks>
    public const string Namespace = "namespace";

    /// <summary>
    /// The name of the property Chronicle stamps with the namespace on every element in <see cref="GraphDatabaseMode.Single"/> mode.
    /// </summary>
    public const string NamespaceProperty = "_namespace";
}
