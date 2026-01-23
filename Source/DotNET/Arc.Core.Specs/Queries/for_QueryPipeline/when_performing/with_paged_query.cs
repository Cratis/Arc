// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_paged_query : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    object _queryData;
    QueryRendererResult _rendererResult;
    QueryResult _result;

    void Establish()
    {
        _queryName = "PagedQuery";
        _parameters = new() { { "filter", "test" } };
        _paging = new Paging(2, 10, true);
        _sorting = new Sorting("Name", SortDirection.Ascending);

        _filterResult = QueryResult.Success(_correlationId);
        _queryData = new[] { new { id = 1 }, new { id = 2 } };
        _rendererResult = new QueryRendererResult(150, _queryData);

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_queryData));
        _queryRenderers.Render(_queryName, _queryData, _serviceProvider).Returns(_rendererResult);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_set_rendered_data_on_result() => _result.Data.ShouldEqual(_rendererResult.Data);
    [Fact] void should_set_paging_info_with_correct_page() => _result.Paging.Page.ShouldEqual(2);
    [Fact] void should_set_paging_info_with_correct_size() => _result.Paging.Size.ShouldEqual(10);
    [Fact] void should_set_paging_info_with_total_items_from_renderer() => _result.Paging.TotalItems.ShouldEqual(150);
}