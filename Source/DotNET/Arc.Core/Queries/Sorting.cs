// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents sorting for a query.
/// </summary>
/// <param name="Field">Field to sort.</param>
/// <param name="Direction">The <see cref="SortDirection"/>.</param>
public record Sorting(string Field, SortDirection Direction)
{
    /// <summary>
    /// Represents no sorting.
    /// </summary>
    public static readonly Sorting None = new(string.Empty, SortDirection.Ascending);
}