// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_exception_during_performance : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    Exception _exception;
    QueryResult _result;

    void Establish()
    {
        _queryName = "QueryWithException";
        _parameters = new() { { "id", 42 } };
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = QueryResult.Success(_correlationId);
        _exception = new InvalidOperationException("Something went wrong");

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromException<object?>(_exception));
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_unsuccessful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exception_message() => _result.ExceptionMessages.ShouldContain(_exception.Message);
    [Fact] void should_have_exception_stack_trace() => _result.ExceptionStackTrace.ShouldEqual(_exception.StackTrace ?? string.Empty);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_call_query_renderers() => _queryRenderers.DidNotReceiveWithAnyArgs().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());
}