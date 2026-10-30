// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the <see cref="QueryableElementOperations"/> for a specific element type.
/// </summary>
/// <typeparam name="TElement">The element type of the queryable.</typeparam>
internal sealed class QueryableElementOperationsFor<TElement> : QueryableElementOperations
{
    /// <inheritdoc/>
    public override int Count(IQueryable queryable) => Queryable.Count((IQueryable<TElement>)queryable);

    /// <inheritdoc/>
    public override IQueryable Skip(IQueryable queryable, int count) => Queryable.Skip((IQueryable<TElement>)queryable, count);

    /// <inheritdoc/>
    public override IQueryable Take(IQueryable queryable, int count) => Queryable.Take((IQueryable<TElement>)queryable, count);
}
