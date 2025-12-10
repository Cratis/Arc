// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Concept representing the name of a query.
/// </summary>
/// <param name="Value">The name of the query.</param>
public record QueryName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to a <see cref="QueryName"/>.
    /// </summary>
    /// <param name="name">The name of the query.</param>
    public static implicit operator QueryName(string name) => new(name);
}
