// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a base for a narrowing of one read model type that keeps the rows whose key a policy allows.
/// </summary>
/// <typeparam name="TReadModel">The read model type the narrowing applies to.</typeparam>
/// <typeparam name="TKey">The type of the key that identifies the recorded state the decision is made from.</typeparam>
/// <remarks>
/// The allowed keys are resolved once per query, from recorded state, and become a store predicate
/// (<c>allowed.Contains(row.Key)</c>), so the store filters before it pages and counts.
/// </remarks>
public abstract class QueryRowNarrowing<TReadModel, TKey> : IQueryRowNarrowing
{
    /// <summary>
    /// Gets the expression that selects the key of a row.
    /// </summary>
    protected abstract Expression<Func<TReadModel, TKey>> Key { get; }

    /// <inheritdoc/>
    public async ValueTask<LambdaExpression?> GetPredicate(Type readModelType, QueryContext context)
    {
        if (readModelType != typeof(TReadModel) || !AppliesTo(context))
        {
            return null;
        }

        var key = Key;
        var allowed = (await GetAllowedKeys(context)).ToList();
        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TKey)],
            Expression.Constant(allowed, typeof(IEnumerable<TKey>)),
            key.Body);
        return Expression.Lambda<Func<TReadModel, bool>>(contains, key.Parameters);
    }

    /// <summary>
    /// Gets whether the narrowing applies to the query being performed.
    /// </summary>
    /// <param name="context">The <see cref="QueryContext"/> of the query.</param>
    /// <returns>True when it applies; false otherwise.</returns>
    protected virtual bool AppliesTo(QueryContext context) => true;

    /// <summary>
    /// Gets the keys of the rows the caller may see. A key with no recorded state is not allowed.
    /// </summary>
    /// <param name="context">The <see cref="QueryContext"/> of the query.</param>
    /// <returns>The allowed keys.</returns>
    protected abstract ValueTask<IEnumerable<TKey>> GetAllowedKeys(QueryContext context);
}
