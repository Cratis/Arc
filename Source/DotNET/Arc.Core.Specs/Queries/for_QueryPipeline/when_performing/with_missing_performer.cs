// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_missing_performer : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _result;

    void Establish()
    {
        _queryName = "NonExistentQuery";
        _parameters = new() { { "id", 42 } };
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = null;
            return false;
        });
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_unsuccessful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exception_message_about_missing_performer() => _result.ExceptionMessages.ShouldContain($"No performer found for query {_queryName}");
    [Fact] void should_not_call_query_filters() => query_filters.DidNotReceiveWithAnyArgs().OnPerform(Arg.Any<QueryContext>());
    [Fact] void should_not_call_query_performer() => _queryPerformer.DidNotReceiveWithAnyArgs().Perform(Arg.Any<QueryContext>());
    [Fact] void should_not_call_query_renderers() => _queryRenderers.DidNotReceiveWithAnyArgs().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());
}