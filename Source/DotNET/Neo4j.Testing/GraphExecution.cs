// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Testing;

/// <summary>
/// Represents a query that was run on a <see cref="GraphSessionSubstitute"/>.
/// </summary>
/// <param name="Cypher">The Cypher text.</param>
/// <param name="Parameters">The parameters as they were given to the driver, concepts already unwrapped and including the namespace.</param>
public record GraphExecution(string Cypher, IReadOnlyDictionary<string, object?> Parameters);
