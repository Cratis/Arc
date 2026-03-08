// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_no_matching_dependency_resolver_falls_back_to_service_provider : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _arguments;
    QueryResult _result;
    object _serviceProviderDependency;
    IQueryDependencyResolver _resolver;
    QueryRendererResult _rendererResult;
    object _queryData;
    IEnumerable<object> _capturedDependencies;

    void Establish()
    {
        _queryName = "TestQuery";
        _arguments = new() { { "id", 42 } };
        _serviceProviderDependency = new object();
        _queryData = new { name = "Test Data" };
        _rendererResult = new QueryRendererResult(1, _queryData);

        _resolver = Substitute.For<IQueryDependencyResolver>();
        _resolver.CanResolve(typeof(object)).Returns(false);

        _queryPerformer.Dependencies.Returns([typeof(object)]);
        _serviceProvider.GetService(typeof(object)).Returns(_serviceProviderDependency);
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Do<QueryContext>(ctx => _capturedDependencies = ctx.Dependencies)).Returns(QueryResult.Success(_correlationId));
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_queryData));
        _queryRenderers.Render(_queryName, _queryData, _serviceProvider).Returns(_rendererResult);

        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            query_filters,
            _queryPerformerProviders,
            _queryRenderers,
            [_resolver]);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _arguments, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_dependency_from_service_provider() => _capturedDependencies.ShouldContainOnly([_serviceProviderDependency]);
    [Fact] void should_not_call_resolver_resolve() =>
        _resolver.DidNotReceive().Resolve(Arg.Any<Type>(), Arg.Any<QueryArguments>(), Arg.Any<IServiceProvider>());
}
