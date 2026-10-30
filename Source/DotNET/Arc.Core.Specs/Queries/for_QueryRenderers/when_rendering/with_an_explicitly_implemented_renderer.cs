// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class with_an_explicitly_implemented_renderer : given.query_renderers
{
    public record Query;

    public sealed class QueryRenderer : IQueryRendererFor<Query>
    {
        public static readonly QueryRendererResult Result = new(7, "rendered");

        QueryRendererResult IQueryRendererFor<Query>.Execute(Query query, QueryContext queryContext) => Result;
    }

    QueryRendererResult _result;

    void Establish()
    {
        Register(new QueryRenderer());
        Construct(typeof(QueryRenderer));
    }

    void Because() => _result = _renderers.Render("Test Query", new Query(), _serviceProvider);

    [Fact] void should_return_the_result_of_the_renderer() => _result.ShouldBeSame(QueryRenderer.Result);
}
