// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class with_a_renderer_that_is_not_registered : given.query_renderers
{
    public record Query;

    public class QueryRenderer : IQueryRendererFor<Query>
    {
        public QueryRendererResult Execute(Query query, QueryContext queryContext) => new(7, "rendered");
    }

    Exception _exception;

    void Establish() => Construct(typeof(QueryRenderer));

    void Because() => _exception = Catch.Exception(() => _renderers.Render("Test Query", new Query(), _serviceProvider));

    [Fact] void should_fail_because_the_renderer_is_not_registered() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
