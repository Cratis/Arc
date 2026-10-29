// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing;

public class and_the_action_captures_an_exception_with_details_hidden : given.a_controller_query_request
{
    QueryResult _result;

    void Establish()
    {
        _options.ExposeExceptionDetails = false;
        _executed.Exception = new Exception("Query failed", new Exception("Inner failure"));
    }

    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        _result = (QueryResult)((ObjectResult)(_actionContext.Result ?? _executed.Result)!).Value!;
    }

    [Fact] void should_hide_the_exception_chain() => _result.ExceptionMessages.ShouldEqual([ExceptionDetailRedactor.RedactedMessage]);
    [Fact] void should_clear_the_stack_trace() => _result.ExceptionStackTrace.ShouldEqual(string.Empty);
}
