// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the result coming from performing a query with a collection of items.
/// </summary>
/// <param name="Page">The page number.</param>
/// <param name="Size">The size of the page.</param>
/// <param name="TotalItems">The total number of items.</param>
public record PagingInfo(int Page, int Size, long TotalItems)
{
    /// <summary>
    /// Represents a not paged result.
    /// </summary>
    public static readonly PagingInfo NotPaged = new(0, 0, 0);

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => Size == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / Size);
}
