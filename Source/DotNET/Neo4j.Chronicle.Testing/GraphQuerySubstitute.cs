// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.Testing;

/// <summary>
/// Represents the <see cref="IGraphQuery{TView}"/> of a <see cref="GraphSubstitute"/>: it builds the query as production
/// does and answers it with the views set up.
/// </summary>
/// <typeparam name="TView">The type of the view.</typeparam>
/// <param name="query">The <see cref="IGraphQuery{TView}"/> that builds the clauses.</param>
/// <param name="answer">Records a query and gives the views it answers with.</param>
internal sealed class GraphQuerySubstitute<TView>(IGraphQuery<TView> query, Func<GraphQuery, IReadOnlyList<TView>> answer) : IGraphQuery<TView>
    where TView : class
{
    /// <inheritdoc/>
    public IGraphQuery<TView> Where<TValue>(Expression<Func<TView, TValue>> property, TValue value)
    {
        query.Where(property, value);
        return this;
    }

    /// <inheritdoc/>
    public IGraphQuery<TView> WhereRelated(Expression<Func<TView, object?>> relation)
    {
        query.WhereRelated(relation);
        return this;
    }

    /// <inheritdoc/>
    public IGraphQuery<TView> WhereRelated<TRelated>(Expression<Func<TView, object?>> relation, Action<IGraphRelatedClauses<TRelated>> clauses)
        where TRelated : class
    {
        query.WhereRelated(relation, clauses);
        return this;
    }

    /// <inheritdoc/>
    public GraphQuery ToQuery(GraphQueryShape shape) => query.ToQuery(shape);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TView>> ToList(CancellationToken cancellationToken = default) =>
        Task.FromResult(answer(ToQuery(GraphQueryShape.List)));

    /// <inheritdoc/>
    public Task<TView?> FirstOrDefault(CancellationToken cancellationToken = default)
    {
        var results = answer(ToQuery(GraphQueryShape.First));
        return Task.FromResult(results.Count == 0 ? null : results[0]);
    }
}
