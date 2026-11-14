// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Represents a graph query translated to Cypher.
/// </summary>
/// <param name="Text">The Cypher text. It holds no values, only quoted identifiers and parameter references.</param>
/// <param name="Parameters">The values of the parameters the text references, converted for the driver. The namespace is not among them: <see cref="IGraphSession"/> adds it.</param>
public sealed record GraphCypherQuery(string Text, IReadOnlyDictionary<string, object?> Parameters);
