// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_rejecting_invalid_arguments;

public class over_sse : given.a_controller_query_request
{
    string[] _events;
    QueryResult _result;

    void Establish()
    {
        InvalidArguments();
        _httpContext.Request.Headers.Accept = "text/event-stream";
    }

    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        _events = ResponseBody().Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        _result = Deserialize(_events.Single()["data: ".Length..]);
    }

    [Fact] void should_send_exactly_one_event() => _events.Length.ShouldEqual(1);
    [Fact] void should_use_the_data_event_format() => _events.Single().StartsWith("data: ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_end_the_event() => ResponseBody().EndsWith("\n\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_invalid_arguments() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_include_the_validation_message() => _result.ValidationResults.Single().Message.ShouldEqual("Must be non-negative");
    [Fact] void should_include_the_invalid_member() => _result.ValidationResults.Single().Members.ShouldEqual(["minimumBalance"]);
    [Fact] void should_set_the_sse_content_type() => _httpContext.Response.ContentType.ShouldEqual("text/event-stream; charset=utf-8");
    [Fact] void should_disable_caching() => _httpContext.Response.Headers.CacheControl.ToString().ShouldEqual("no-cache");
    [Fact] void should_disable_proxy_buffering() => _httpContext.Response.Headers["X-Accel-Buffering"].ToString().ShouldEqual("no");
    [Fact] void should_keep_http_success() => _httpContext.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_not_execute_the_action() => _next.DidNotReceive().Invoke();
    [Fact] void should_suppress_mvc_response_writing() => _actionContext.Result.ShouldBeOfExactType<EmptyResult>();
    [Fact] void should_not_open_a_websocket() => _transportEvents.ShouldBeEmpty();
}
