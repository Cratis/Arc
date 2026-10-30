// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_IQueryRendererFor;

public class when_executing_through_the_non_generic_interface : Specification
{
    public class TextRenderer : IQueryRendererFor<string>
    {
        public static readonly QueryRendererResult Result = new(7, "rendered");

        public string ReceivedQuery;
        public QueryContext ReceivedContext;

        public QueryRendererResult Execute(string query, QueryContext queryContext)
        {
            ReceivedQuery = query;
            ReceivedContext = queryContext;
            return Result;
        }
    }

    TextRenderer _renderer;
    QueryContext _context;
    QueryRendererResult _result;

    void Establish()
    {
        _renderer = new TextRenderer();
        _context = new("Test Query", CorrelationId.New(), Paging.NotPaged, Sorting.None);
    }

    void Because() => _result = ((IQueryRenderer)_renderer).Execute("text", _context);

    [Fact] void should_give_the_query_to_the_typed_renderer() => _renderer.ReceivedQuery.ShouldEqual("text");
    [Fact] void should_give_the_context_to_the_typed_renderer() => _renderer.ReceivedContext.ShouldBeSame(_context);
    [Fact] void should_return_the_result_of_the_typed_renderer() => _result.ShouldBeSame(TextRenderer.Result);
}
