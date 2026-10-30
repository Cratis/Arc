// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the <see cref="QueryableSortKey"/> for a specific element type and sort key type.
/// </summary>
/// <typeparam name="TElement">The element type of the queryable.</typeparam>
/// <typeparam name="TKey">The type of the property to sort on.</typeparam>
internal sealed class QueryableSortKeyFor<TElement, TKey> : QueryableSortKey
{
    /// <inheritdoc/>
    public override IQueryable OrderBy(IQueryable queryable, PropertyInfo property, SortDirection direction) =>
        QueryableExtensions.OrderByProperty<TElement, TKey>(queryable, property, direction);
}
