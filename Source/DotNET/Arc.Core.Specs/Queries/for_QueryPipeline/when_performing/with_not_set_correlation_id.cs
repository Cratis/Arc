// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_not_set_correlation_id : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    object _queryData;
    QueryRendererResult _rendererResult;
    QueryResult _result;
    QueryContext _capturedContext;

    void Establish()
    {
        _queryName = "TestQuery";
        _parameters = new() { { "id", 42 } };
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _correlationIdAccessor.Current.Returns(CorrelationId.NotSet);

        _filterResult = QueryResult.Success(CorrelationId.NotSet);
        _queryData = new { name = "Test Data" };
        _rendererResult = new QueryRendererResult(100, new { renderedData = "Rendered Test Data" });

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Do<QueryContext>(ctx => _capturedContext = ctx)).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(Task.FromResult<object?>(_queryData));
        _queryRenderers.Render(_queryName, _queryData, _serviceProvider).Returns(_rendererResult);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_generate_new_correlation_id_for_context() => _capturedContext.CorrelationId.ShouldNotEqual(CorrelationId.NotSet);
    [Fact] void should_use_generated_correlation_id_consistently() => _capturedContext.CorrelationId.Value.ShouldNotEqual(Guid.Empty);
}