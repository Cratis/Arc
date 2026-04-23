// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_single_item_result_and_interceptor : given.a_query_action_filter
{
    TestReadModel _responseData;
    QueryRendererResult _rendererResult;

    void Establish()
    {
        _responseData = new TestReadModel("single");
        _rendererResult = new QueryRendererResult(1, _responseData);

        var queryContext = new QueryContext("[TestApp].[TestQuery]", Cratis.Execution.CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _queryContextManager.Current.Returns(queryContext);

        _queryRenderers.Render(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Any<object>(),
            Arg.Any<IServiceProvider>())
            .Returns(_rendererResult);

        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(Task.CompletedTask);
    }

    async Task Because()
    {
        async Task<ActionExecutedContext> next() =>
            new(_actionContext, [], null!)
            {
                Result = new ObjectResult(_responseData)
            };

        await _filter.OnActionExecutionAsync(_actionContext, next);
    }

    [Fact] void should_call_interceptor_for_the_item() =>
        _readModelInterceptors.Received(1).Intercept(typeof(TestReadModel), Arg.Is<IEnumerable<object>>(items => items.Contains(_responseData)), Arg.Any<IServiceProvider>());
}
