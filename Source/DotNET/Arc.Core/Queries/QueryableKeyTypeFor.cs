// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq.Expressions;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the <see cref="QueryableKeyType"/> for a specific key type.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
internal sealed class QueryableKeyTypeFor<TKey> : QueryableKeyType
{
    /// <inheritdoc/>
    public override IEqualityComparer EqualityComparer => EqualityComparer<TKey>.Default;

    /// <inheritdoc/>
    public override IComparer Comparer => Comparer<TKey>.Default;

    /// <inheritdoc/>
    public override IQueryable<TElement> OrderBy<TElement>(IQueryable<TElement> queryable, Expression keyBody, ParameterExpression parameter, SortDirection direction)
    {
        var keySelector = Expression.Lambda<Func<TElement, TKey>>(keyBody, parameter);
        return direction == SortDirection.Ascending
            ? queryable.OrderBy(keySelector)
            : queryable.OrderByDescending(keySelector);
    }
}
