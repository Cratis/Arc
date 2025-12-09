// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_failed_filters : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    QueryResult _result;

    void Establish()
    {
        _queryName = "FilteredQuery";
        _parameters = new() { { "id", 42 } };
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = new QueryResult
        {
            CorrelationId = _correlationId,
            IsAuthorized = false
        };

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting);

    [Fact] void should_return_filter_result() => _result.ShouldEqual(_filterResult);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_call_query_performer() => _queryPerformer.DidNotReceiveWithAnyArgs().Perform(Arg.Any<QueryContext>());
    [Fact] void should_not_call_query_renderers() => _queryRenderers.DidNotReceiveWithAnyArgs().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>());
}