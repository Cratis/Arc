// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents sorting a non-generic <see cref="IQueryable"/> for one element type and one sort key type.
/// </summary>
internal abstract class QueryableSortKey
{
    /// <summary>
    /// Sorts the elements of a sequence in ascending or descending order according to a property.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to sort.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> of the property to sort on.</param>
    /// <param name="direction">The <see cref="SortDirection"/>.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public abstract IQueryable OrderBy(IQueryable queryable, PropertyInfo property, SortDirection direction);
}
