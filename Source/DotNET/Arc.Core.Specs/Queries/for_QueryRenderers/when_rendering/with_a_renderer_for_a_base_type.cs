// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class with_a_renderer_for_a_base_type : given.query_renderers
{
    public interface IQuery;

    public record Query : IQuery;

    public class QueryRenderer : IQueryRendererFor<IQuery>
    {
        public static readonly QueryRendererResult Result = new(7, "rendered");

        public QueryRendererResult Execute(IQuery query, QueryContext queryContext) => Result;
    }

    QueryRendererResult _result;

    void Establish()
    {
        Register(new QueryRenderer());
        Construct(typeof(QueryRenderer));
    }

    void Because() => _result = _renderers.Render("Test Query", new Query(), _serviceProvider);

    [Fact] void should_select_the_renderer_for_the_base_type() => _result.ShouldBeSame(QueryRenderer.Result);
}
