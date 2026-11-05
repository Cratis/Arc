// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines how Arc places the graphs of the different namespaces in Neo4j.
/// </summary>
public enum GraphDatabaseMode
{
    /// <summary>
    /// One graph database per event store and namespace, named by <see cref="GraphDatabaseName"/>. This is the mode for production.
    /// </summary>
    PerNamespace = 0,

    /// <summary>
    /// One graph database for every namespace, with the namespace stamped as <c>_namespace</c> on every node and relationship.
    /// </summary>
    /// <remarks>
    /// Meant for development and test, where a Neo4j edition without multiple databases is common. Nothing isolates a
    /// namespace from the others except the filter a query applies, so filter on <c>_namespace</c> using the
    /// <see cref="GraphParameters.Namespace"/> parameter that <see cref="IGraphSession"/> adds.
    /// </remarks>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Single is the name of the mode in the configuration and in Chronicle; it describes one database, not a type.")]
    Single = 1
}
