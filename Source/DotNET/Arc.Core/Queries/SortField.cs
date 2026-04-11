// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Concept representing the name of a field to sort by.
/// </summary>
/// <param name="Value">Concept value.</param>
public record SortField(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents a not-set <see cref="SortField"/>.
    /// </summary>
    public static readonly SortField NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="SortField"/>.
    /// </summary>
    /// <param name="value">The string to convert from.</param>
    public static implicit operator SortField(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="SortField"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="sortField">The <see cref="SortField"/> to convert from.</param>
    public static implicit operator string(SortField sortField) => sortField.Value;
}
