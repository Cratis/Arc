// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Writes the labels, relationship types, property names and member names of a graph view into Cypher text.
/// </summary>
/// <remarks>
/// They come from the declared graph and views, never from a value, and every one is quoted with backticks. An identifier
/// that is empty or whitespace, or that holds a backtick or a control character, is refused instead of escaped, so nothing
/// that reaches the text can change the shape of the query.
/// </remarks>
public static class CypherIdentifiers
{
    /// <summary>
    /// Quote an identifier for Cypher text.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The identifier in backticks.</returns>
    /// <exception cref="InvalidCypherIdentifier">Thrown when the identifier cannot be quoted safely.</exception>
    public static string Quote(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Any(_ => _ == '`' || char.IsControl(_)))
        {
            throw new InvalidCypherIdentifier(identifier);
        }

        return $"`{identifier}`";
    }
}
