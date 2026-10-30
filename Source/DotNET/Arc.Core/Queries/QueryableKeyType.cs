// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq.Expressions;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the default comparers of one key type, such as an id type or the type of a property to sort on.
/// </summary>
internal abstract class QueryableKeyType
{
    /// <summary>
    /// Gets the default <see cref="IEqualityComparer"/> of the key type.
    /// </summary>
    public abstract IEqualityComparer EqualityComparer { get; }

    /// <summary>
    /// Gets the default <see cref="IComparer"/> of the key type.
    /// </summary>
    public abstract IComparer Comparer { get; }

    /// <summary>
    /// Sorts the elements of a typed sequence in ascending or descending order according to a key expression of the key type.
    /// </summary>
    /// <typeparam name="TElement">The element type of the queryable.</typeparam>
    /// <param name="queryable">The <see cref="IQueryable{T}"/> to sort.</param>
    /// <param name="keyBody">The <see cref="Expression"/> selecting the key, whose type is the key type.</param>
    /// <param name="parameter">The <see cref="ParameterExpression"/> the key expression reads the element from.</param>
    /// <param name="direction">The <see cref="SortDirection"/>.</param>
    /// <returns>An <see cref="IQueryable{T}"/> for continuation.</returns>
    public abstract IQueryable<TElement> OrderBy<TElement>(IQueryable<TElement> queryable, Expression keyBody, ParameterExpression parameter, SortDirection direction)
        where TElement : class;
}
