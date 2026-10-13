// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_invalid_collection_over_sse : given.a_query_request
{
    QueryResult _received;

    void Establish()
    {
        _performer.Parameters.Returns(new QueryParameters([new QueryParameter("ids", typeof(int[]))]));
        _context.Query.Returns(new Dictionary<string, string> { ["ids"] = "1,invalid" });
        _context.Headers.Returns(new Dictionary<string, string> { ["Accept"] = "text/event-stream" });
        _context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _received = JsonSerializer.Deserialize<QueryResult>(call.Arg<string>()["data: ".Length..].Trim(), _arcOptions.JsonSerializerOptions)!;
            return Task.CompletedTask;
        });
    }

    Task Because() => _mapper.HandlerFor("GET")(_context);

    [Fact] void should_be_invalid() => _received.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_members() => _received.ValidationResults.Single().Members.ShouldContainOnly("ids");
    [Fact] void should_set_stream_content_type() => _context.ContentType.ShouldEqual("text/event-stream; charset=utf-8");
    [Fact] void should_not_perform_the_query() => _queryPipeline.DidNotReceive().Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
}
