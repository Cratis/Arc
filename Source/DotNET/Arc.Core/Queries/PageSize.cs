// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Concept representing the number of items per page for paging.
/// </summary>
/// <param name="Value">Concept value.</param>
public record PageSize(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Represents a not-set <see cref="PageSize"/>.
    /// </summary>
    public static readonly PageSize NotSet = new(0);

    /// <summary>
    /// Implicitly converts an <see cref="int"/> to a <see cref="PageSize"/>.
    /// </summary>
    /// <param name="value">The integer to convert from.</param>
    public static implicit operator PageSize(int value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="PageSize"/> to an <see cref="int"/>.
    /// </summary>
    /// <param name="pageSize">The <see cref="PageSize"/> to convert from.</param>
    public static implicit operator int(PageSize pageSize) => pageSize.Value;
}
