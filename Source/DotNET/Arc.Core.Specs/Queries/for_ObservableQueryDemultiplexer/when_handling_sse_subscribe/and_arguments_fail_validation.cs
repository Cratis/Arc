// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_arguments_fail_validation : given.an_sse_connection_owned_by_a_caller
{
    QueryResult _rejection;
    QueryResult _received;

    void Establish()
    {
        _rejection = QueryResult.WithValidationError(CorrelationId.New(), "searchTerm", "Search term is required.");
        _rejection.Data = new { Secret = "not for the client" };
        _rejection.ExceptionStackTrace = "internal details";
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_rejection));
    }

    async Task Because()
    {
        await RunConnection(async () => await _hub.HandleSSESubscribe(SubscribeContextFor(_owner)));
        var message = _messages.Select(text => JsonSerializer.Deserialize<ObservableQueryHubMessage>(text["data: ".Length..].Trim(), _arcOptions.Value.JsonSerializerOptions))
            .Single(message => message!.Type == ObservableQueryHubMessageType.QueryResult);
        _received = ((JsonElement)message!.Payload!).Deserialize<QueryResult>(_arcOptions.Value.JsonSerializerOptions)!;
    }

    [Fact] void should_send_one_query_result() => CountQueryResults().ShouldEqual(1);
    [Fact] void should_be_invalid() => _received.IsValid.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _received.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_correlation_id() => _received.CorrelationId.ShouldEqual(_rejection.CorrelationId);
    [Fact] void should_preserve_message() => _received.ValidationResults.Single().Message.ShouldEqual("Search term is required.");
    [Fact] void should_preserve_members() => _received.ValidationResults.Single().Members.ShouldContainOnly("searchTerm");
    [Fact] void should_not_report_an_exception() => _received.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_send_data() => _received.Data.ShouldBeNull();
    [Fact] void should_not_send_internal_details() => _received.ExceptionStackTrace.ShouldBeEmpty();
}
