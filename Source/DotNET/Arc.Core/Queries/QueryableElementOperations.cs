// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the count and paging operations of a non-generic <see cref="IQueryable"/> for one element type.
/// </summary>
internal abstract class QueryableElementOperations
{
    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to count.</param>
    /// <returns>The number of elements in the input sequence.</returns>
    public abstract int Count(IQueryable queryable);

    /// <summary>
    /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to skip in.</param>
    /// <param name="count">The number of elements to skip.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public abstract IQueryable Skip(IQueryable queryable, int count);

    /// <summary>
    /// Returns a specified number of contiguous elements from the start of a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to take from.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public abstract IQueryable Take(IQueryable queryable, int count);
}
