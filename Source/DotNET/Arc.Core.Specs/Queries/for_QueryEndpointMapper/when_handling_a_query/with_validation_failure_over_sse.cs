// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_validation_failure_over_sse : given.a_query_rejected_by_validation
{
    QueryResult _received;

    void Establish()
    {
        _context.Headers.Returns(new Dictionary<string, string> { ["Accept"] = "text/event-stream" });
        _context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _received = JsonSerializer.Deserialize<QueryResult>(call.Arg<string>()["data: ".Length..].Trim(), _arcOptions.JsonSerializerOptions)!;
            return Task.CompletedTask;
        });
    }

    Task Because() => _mapper.HandlerFor("GET")(_context);

    [Fact] void should_set_stream_content_type() => _context.ContentType.ShouldEqual("text/event-stream; charset=utf-8");
    [Fact] void should_send_one_frame() => _context.Received(1).Write(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_be_invalid() => _received.IsValid.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _received.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_message() => _received.ValidationResults.Single().Message.ShouldEqual("Search term is required.");
    [Fact] void should_preserve_members() => _received.ValidationResults.Single().Members.ShouldContainOnly("searchTerm");
    [Fact] void should_preserve_correlation_id() => _received.CorrelationId.ShouldEqual(_rejection.CorrelationId);
    [Fact] void should_not_send_data() => _received.Data.ShouldBeNull();
    [Fact] void should_not_send_an_http_body() => _context.DidNotReceive().WriteResponseAsJson(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>());
}
