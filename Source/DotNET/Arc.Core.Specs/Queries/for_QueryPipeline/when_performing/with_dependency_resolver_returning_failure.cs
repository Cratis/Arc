// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_dependency_resolver_returning_failure : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _arguments;
    QueryResult _result;
    IQueryDependencyResolver _resolver;
    Exception _resolverException;

    void Establish()
    {
        _queryName = "TestQuery";
        _arguments = new() { { "id", 42 } };
        _resolverException = new InvalidOperationException("Resolution failed");

        _resolver = Substitute.For<IQueryDependencyResolver>();
        _resolver.CanResolve(typeof(object)).Returns(true);
        _resolver.Resolve(typeof(object), Arg.Any<QueryArguments>(), Arg.Any<IServiceProvider>())
            .Returns(Catch<object>.Failed(_resolverException));

        _queryPerformer.Dependencies.Returns([typeof(object)]);
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));

        // The real performer iterates context.Dependencies, which triggers the resolver.
        // Simulate that behavior so the RethrowError() propagates into the pipeline's catch block.
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(callInfo =>
        {
            var context = callInfo.Arg<QueryContext>();
            _ = context.Dependencies?.ToArray();
            return ValueTask.FromResult<object?>(null);
        });

        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            query_filters,
            _queryPerformerProviders,
            _queryRenderers,
            [_resolver]);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _arguments, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_contain_exception_message() => _result.ExceptionMessages.ShouldContain(_resolverException.Message);
}
