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

    [Fact] void should_wrap_in_target_invocation_exception() => _exception.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_carry_the_thrown_exception_as_inner() => _exception.InnerException.ShouldBeSame(QueryRenderer.Failure);
}
