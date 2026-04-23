// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_read_model_interceptor : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    List<object> _renderedData;
    QueryRendererResult _rendererResult;
    QueryResult _result;

    void Establish()
    {
        _queryName = "TestQuery";
        _parameters = new() { { "id", 42 } };
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = QueryResult.Success(_correlationId);
        _renderedData = [new { value = "item-one" }, new { value = "item-two" }];
        _rendererResult = new QueryRendererResult(2, _renderedData);

        _queryPerformer.ReadModelType.Returns(typeof(object));
        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_renderedData));
        _queryRenderers.Render(_queryName, _renderedData, _serviceProvider).Returns(_rendererResult);
        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns(Task.CompletedTask);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_call_interceptors_with_all_items() =>
        _readModelInterceptors.Received(1).Intercept(typeof(object), Arg.Any<IEnumerable<object>>(), _serviceProvider);
}
