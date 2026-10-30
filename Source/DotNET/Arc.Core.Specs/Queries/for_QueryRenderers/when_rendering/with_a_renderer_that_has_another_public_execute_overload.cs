// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class with_a_renderer_that_has_another_public_execute_overload : given.query_renderers
{
    public record Query;

    public class QueryRenderer : IQueryRendererFor<Query>
    {
        public static readonly QueryRendererResult Result = new(7, "rendered");

        public QueryRendererResult Execute(Query query, QueryContext queryContext) => Result;

        public QueryRendererResult Execute(string other) => new(0, other);
    }

    QueryRendererResult _result;

    void Establish()
    {
        Register(new QueryRenderer());
        Construct(typeof(QueryRenderer));
    }

    void Because() => _result = _renderers.Render("Test Query", new Query(), _serviceProvider);

    [Fact] void should_run_the_interface_method() => _result.ShouldBeSame(QueryRenderer.Result);
}
