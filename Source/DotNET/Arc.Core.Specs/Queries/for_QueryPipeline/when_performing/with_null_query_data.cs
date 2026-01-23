// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_null_query_data : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    QueryResult _result;

    void Establish()
    {
        _queryName = "QueryWithNullData";
        _parameters = new() { { "id", 42 } };
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = QueryResult.Success(_correlationId);

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(null));
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id_from_filter_result() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_call_query_renderers() => _queryRenderers.DidNotReceiveWithAnyArgs().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());
    [Fact] void should_have_default_data() => _result.Data.ShouldBeNull();
}