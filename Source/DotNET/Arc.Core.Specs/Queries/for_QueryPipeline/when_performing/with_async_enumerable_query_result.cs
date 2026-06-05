// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

/// <summary>
/// Like an observable subject, an async-enumerable result is a streaming wrapper that the streaming transport
/// intercepts per item — the pipeline must leave it untouched rather than passing it to the per-item interceptor.
/// </summary>
public class with_async_enumerable_query_result : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    IAsyncEnumerable<object> _asyncEnumerable;
    QueryRendererResult _rendererResult;
    QueryResult _result;

    void Establish()
    {
        _queryName = "TestQuery";
        _parameters = new();
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = QueryResult.Success(_correlationId);
        _asyncEnumerable = CreateAsyncStream();
        _rendererResult = new QueryRendererResult(0, _asyncEnumerable);

        _queryPerformer.ReadModelType.Returns(typeof(object));
        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_asyncEnumerable));
        _queryRenderers.Render(_queryName, _asyncEnumerable, _serviceProvider).Returns(_rendererResult);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_capture_any_exception() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_return_the_async_enumerable_untouched() => _result.Data.ShouldEqual(_asyncEnumerable);
    [Fact] void should_not_invoke_the_read_model_interceptors() =>
        _readModelInterceptors.DidNotReceive().Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>());

    static async IAsyncEnumerable<object> CreateAsyncStream()
    {
        await Task.CompletedTask;
        yield return new { value = "item-one" };
    }
}
