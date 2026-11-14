// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.Testing;

/// <summary>
/// Represents an <see cref="IGraph"/> for specifications that answers view queries with the views you set up, without a Neo4j server.
/// </summary>
/// <remarks>
/// <para>
/// Set up what a view answers with <see cref="Returns{TView}"/>, then read back what was asked with <see cref="Executions"/>.
/// A view nothing was set up for answers with no views. Clauses are built exactly as in production, so a selector that does
/// not name a member of the view, or a type that is not a valid view, fails the same way; but the substitute does not
/// evaluate them: <c>ToList</c> answers with every view set up and <c>FirstOrDefault</c> with the first one. Assert on the
/// recorded <see cref="GraphQuery"/> to prove what a query selects.
/// </para>
/// <para>
/// To evaluate the clauses against an in-memory graph, use the <c>GraphScenario</c> of Cratis.Chronicle.Testing and hand
/// its <c>Graph</c> to <c>UseGraph</c>.
/// </para>
/// </remarks>
/// <param name="views">The <see cref="IGraphViews"/> the queried views are declared in.</param>
public sealed class GraphSubstitute(IGraphViews views) : IGraph
{
    readonly Dictionary<Type, IReadOnlyList<object>> _results = [];
    readonly List<GraphQuery> _executions = [];
    readonly QueryableGraph _graph = new(views, new UnusedExecutor());

    /// <summary>
    /// Gets the queries that were run, in order, as the backend-neutral query an executor would get.
    /// </summary>
    public IReadOnlyList<GraphQuery> Executions => _executions;

    /// <summary>
    /// Set up the views a query for a view answers with.
    /// </summary>
    /// <typeparam name="TView">The type of the view.</typeparam>
    /// <param name="results">The views to answer with, in order.</param>
    /// <returns>The substitute for continuation.</returns>
    public GraphSubstitute Returns<TView>(params TView[] results)
        where TView : class
    {
        _results[typeof(TView)] = results;
        return this;
    }

    /// <inheritdoc/>
    public IGraphQuery<TView> Nodes<TView>()
        where TView : class => new GraphQuerySubstitute<TView>(_graph.Nodes<TView>(), Answer<TView>);

    IReadOnlyList<TView> Answer<TView>(GraphQuery query)
    {
        _executions.Add(query);
        return _results.TryGetValue(typeof(TView), out var results) ? [.. results.Cast<TView>()] : [];
    }

    sealed class UnusedExecutor : IGraphQueryExecutor
    {
        public Task<IReadOnlyList<GraphRow>> Execute(GraphQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The graph substitute answers queries itself.");
    }
}
