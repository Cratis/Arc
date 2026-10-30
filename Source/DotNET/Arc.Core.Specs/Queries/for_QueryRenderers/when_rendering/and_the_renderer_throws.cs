// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryRenderers.when_rendering;

public class and_the_renderer_throws : given.query_renderers
{
    public record Query;

    public class RendererFailed : Exception;

    public class QueryRenderer : IQueryRendererFor<Query>
    {
        public static readonly RendererFailed Failure = new();

        public QueryRendererResult Execute(Query query, QueryContext queryContext) => throw Failure;
    }

    Exception _exception;

    void Establish()
    {
        Register(new QueryRenderer());
        Construct(typeof(QueryRenderer));
    }

    void Because() => _exception = Catch.Exception(() => _renderers.Render("Test Query", new Query(), _serviceProvider));

    [Fact] void should_throw_the_exception_of_the_renderer() => _exception.ShouldBeSame(QueryRenderer.Failure);
    [Fact] void should_not_wrap_in_target_invocation_exception() => (_exception is TargetInvocationException).ShouldBeFalse();
}
