// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class with_a_matching_renderer : given.query_renderers
{
    public record Query;

    public class QueryRenderer : IQueryRendererFor<Query>
    {
        public static readonly QueryRendererResult Result = new(7, "rendered");

        public Query ReceivedQuery;
        public QueryContext ReceivedContext;

        public QueryRendererResult Execute(Query query, QueryContext queryContext)
        {
            ReceivedQuery = query;
            ReceivedContext = queryContext;
            return Result;
        }
    }

    Query _query;
    QueryRenderer _renderer;
    QueryRendererResult _result;

    void Establish()
    {
        _query = new Query();
        _renderer = new QueryRenderer();
        Register(_renderer);
        Construct(typeof(QueryRenderer));
    }

    void Because() => _result = _renderers.Render("Test Query", _query, _serviceProvider);

    [Fact] void should_return_the_result_of_the_renderer() => _result.ShouldBeSame(QueryRenderer.Result);
    [Fact] void should_give_the_query_to_the_renderer() => _renderer.ReceivedQuery.ShouldBeSame(_query);
    [Fact] void should_give_the_current_context_to_the_renderer() => _renderer.ReceivedContext.ShouldBeSame(_queryContext);
}
