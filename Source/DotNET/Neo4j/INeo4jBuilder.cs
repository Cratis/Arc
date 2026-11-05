// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines the builder for Neo4j.
/// </summary>
public interface INeo4jBuilder
{
    /// <summary>
    /// Gets or sets the type of the <see cref="IGraphNamespaceResolver"/>.
    /// </summary>
    Type NamespaceResolverType { get; set; }

    /// <summary>
    /// Gets or sets the type of the <see cref="IGraphDatabaseNameResolver"/>.
    /// </summary>
    Type DatabaseNameResolverType { get; set; }

    /// <summary>
    /// Validate the builder.
    /// </summary>
    void Validate();
}
