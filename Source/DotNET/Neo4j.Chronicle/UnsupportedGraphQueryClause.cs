// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// The exception that is thrown when a graph query holds a kind of clause the Cypher translation does not know.
/// </summary>
/// <param name="clause">The <see cref="GraphQueryClause"/> that cannot be translated.</param>
public class UnsupportedGraphQueryClause(GraphQueryClause clause)
    : Exception($"The graph query clause '{clause.GetType().Name}' cannot be translated to Cypher.");
