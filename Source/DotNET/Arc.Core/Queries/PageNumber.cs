// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Concept representing the page number (zero-based) for paging.
/// </summary>
/// <param name="Value">Concept value.</param>
public record PageNumber(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Represents a not-set <see cref="PageNumber"/>.
    /// </summary>
    public static readonly PageNumber NotSet = new(0);

    /// <summary>
    /// Implicitly converts an <see cref="int"/> to a <see cref="PageNumber"/>.
    /// </summary>
    /// <param name="value">The integer to convert from.</param>
    public static implicit operator PageNumber(int value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="PageNumber"/> to an <see cref="int"/>.
    /// </summary>
    /// <param name="pageNumber">The <see cref="PageNumber"/> to convert from.</param>
    public static implicit operator int(PageNumber pageNumber) => pageNumber.Value;
}
