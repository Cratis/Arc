// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// The exception that is thrown when a Cypher result is not the map the translation of a graph view query returns.
/// </summary>
/// <param name="view">The name of the view the row was read for.</param>
/// <param name="value">The value that was returned.</param>
public class UnexpectedGraphRow(string view, object? value)
    : Exception($"Expected a map for a row of the view '{view}', but the query returned '{value?.GetType().Name ?? "null"}'.");
