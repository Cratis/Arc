// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_rejecting_invalid_arguments;

public class over_websocket : given.a_controller_query_request
{
    QueryResult _result;

    void Establish()
    {
        InvalidArguments();
        _webSocketFeature.IsWebSocketRequest.Returns(true);
    }

    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        _result = Deserialize(_frames.Single());
    }

    [Fact] void should_accept_send_one_frame_then_close() => _transportEvents.ShouldEqual(["accept", "send", "close"]);
    [Fact] void should_report_invalid_arguments() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_include_the_validation_message() => _result.ValidationResults.Single().Message.ShouldEqual("Must be non-negative");
    [Fact] void should_include_the_invalid_member() => _result.ValidationResults.Single().Members.ShouldEqual(["minimumBalance"]);
    [Fact] void should_preserve_the_validation_reason() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
    [Fact] void should_send_a_complete_text_frame() => _socket.Received(1).SendAsync(Arg.Any<ArraySegment<byte>>(), System.Net.WebSockets.WebSocketMessageType.Text, true, _httpContext.RequestAborted);
    [Fact] void should_close_normally() => _socket.Received(1).CloseAsync(WebSocketCloseStatus.NormalClosure, "Query validation failed", _httpContext.RequestAborted);
    [Fact] void should_not_set_http_bad_request() => _httpContext.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_not_execute_the_action() => _next.DidNotReceive().Invoke();
    [Fact] void should_suppress_mvc_response_writing() => _actionContext.Result.ShouldBeOfExactType<EmptyResult>();
    [Fact] void should_not_write_an_http_body() => ResponseBody().ShouldEqual(string.Empty);
    [Fact] void should_not_deliver_data() => _result.Data.ShouldBeNull();
}
