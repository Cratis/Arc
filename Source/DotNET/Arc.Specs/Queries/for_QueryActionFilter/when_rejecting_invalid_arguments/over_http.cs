// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_rejecting_invalid_arguments;

public class over_http : given.a_controller_query_request
{
    QueryResult _result;

    void Establish() => InvalidArguments();

    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        _result = (QueryResult)((ObjectResult)_actionContext.Result!).Value!;
    }

    [Fact] void should_keep_http_bad_request() => _httpContext.Response.StatusCode.ShouldEqual(400);
    [Fact] void should_report_invalid_arguments() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_include_the_validation_message() => _result.ValidationResults.Single().Message.ShouldEqual("Must be non-negative");
    [Fact] void should_include_the_invalid_member() => _result.ValidationResults.Single().Members.ShouldEqual(["minimumBalance"]);
    [Fact] void should_not_execute_the_action() => _next.DidNotReceive().Invoke();
    [Fact] void should_leave_serialization_to_mvc() => ResponseBody().ShouldEqual(string.Empty);
    [Fact] void should_not_open_a_stream() => _transportEvents.ShouldBeEmpty();
}
