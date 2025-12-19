// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Concept representing a fully qualified name of a query.
/// </summary>
/// <param name="Value">The fully qualified name of the query.</param>
public record FullyQualifiedQueryName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to a <see cref="FullyQualifiedQueryName"/>.
    /// </summary>
    /// <param name="name">The name of the query.</param>
    public static implicit operator FullyQualifiedQueryName(string name) => new(name);
}
