// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// The exception that is thrown when a label, relationship type, property or member name cannot be written into Cypher text.
/// </summary>
/// <param name="identifier">The identifier that was refused.</param>
public class InvalidCypherIdentifier(string? identifier)
    : Exception($"'{identifier}' cannot be used as a Cypher identifier. An identifier must not be empty or whitespace, and must not contain a backtick or a control character.");
