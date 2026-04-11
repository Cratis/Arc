// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_connection_is_known_and_delta_mode_subsequent_emission : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";

    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    CancellationTokenSource _connectionCancellation;
    ConcurrentQueue<string> _messages;
    BehaviorSubject<IEnumerable<string>> _subject;
    CorrelationId _correlationId;
    string _connectionId;

    void Establish()
    {
        _connectionCancellation = new CancellationTokenSource();
        _messages = [];
        _connectionId = string.Empty;

        _subject = new BehaviorSubject<IEnumerable<string>>([]);
        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                _correlationId = CorrelationId.New();
                var queryResult = QueryResult.Success(_correlationId);
                queryResult.Data = _subject;
                return Task.FromResult(queryResult);
            });

        _connectionContext = Substitute.For<IHttpRequestContext>();
        _connectionContext.RequestAborted.Returns(_connectionCancellation.Token);
        _connectionContext.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _messages.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });

        _subscribeContext = Substitute.For<IHttpRequestContext>();
        _subscribeContext.RequestAborted.Returns(CancellationToken.None);
        _subscribeContext.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId,
                QueryId,
                new ObservableQuerySubscriptionRequest(ControllerQueryName, TransferMode: "delta"))));
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);

        await WaitFor(() => TryExtractConnectionId(out _connectionId));
        await _hub.HandleSSESubscribe(_subscribeContext);

        // First push — full snapshot sent, no ChangeSet.
        _subject.OnNext(["item-a"]);
        await WaitFor(() => CountQueryResultMessages() >= 1);

        // Second push — item-b added; delta mode sends only the ChangeSet, no Data.
        _subject.OnNext(["item-a", "item-b"]);
        await WaitFor(() => CountQueryResultMessages() >= 2);

        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    [Fact] void should_have_no_data_on_second_result() => SecondQueryResultData().ShouldBeNull();
    [Fact] void should_include_change_set_on_second_result() => SecondQueryResultChangeSet().ShouldNotBeNull();
    [Fact] void should_have_one_added_item_in_change_set() => SecondQueryResultAddedCount().ShouldEqual(1);
    [Fact] void should_have_no_removed_items_in_change_set() => SecondQueryResultRemovedCount().ShouldEqual(0);
    [Fact] void should_carry_correlation_id_on_second_result() => SecondQueryResultCorrelationId().ShouldEqual(_correlationId);

    QueryResult? SecondQueryResult()
    {
        var results = GetQueryResults();
        return results.Count >= 2 ? results[1] : null;
    }

    object? SecondQueryResultData() => SecondQueryResult()?.Data;

    ChangeSet? SecondQueryResultChangeSet() => SecondQueryResult()?.ChangeSet;

    int SecondQueryResultAddedCount() => SecondQueryResultChangeSet()?.Added.Count() ?? -1;
    int SecondQueryResultRemovedCount() => SecondQueryResultChangeSet()?.Removed.Count() ?? -1;

    CorrelationId SecondQueryResultCorrelationId() => SecondQueryResult()?.CorrelationId ?? new CorrelationId(Guid.Empty);

    int CountQueryResultMessages() => GetQueryResults().Count;

    List<QueryResult> GetQueryResults()
    {
        var results = new List<QueryResult>();

        foreach (var hubMessage in _messages
                     .Select(TryParseHubMessage)
                     .Where(_ => _ is not null)
                     .Select(_ => _!))
        {
            if (hubMessage.Type != ObservableQueryHubMessageType.QueryResult ||
                hubMessage.QueryId != QueryId ||
                hubMessage.Payload is not JsonElement payload ||
                payload.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var queryResult = JsonSerializer.Deserialize<QueryResult>(payload.GetRawText(), _arcOptions.Value.JsonSerializerOptions);
            if (queryResult is not null)
            {
                results.Add(queryResult);
            }
        }

        return results;
    }

    bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;

        foreach (var hubMessage in _messages
                     .Select(TryParseHubMessage)
                     .Where(_ => _ is not null)
                     .Select(_ => _!))
        {
            if (hubMessage.Type != ObservableQueryHubMessageType.Connected || hubMessage.Payload is not JsonElement payload)
            {
                continue;
            }

            connectionId = payload.GetString() ?? string.Empty;
            return !string.IsNullOrEmpty(connectionId);
        }

        return false;
    }

    ObservableQueryHubMessage? TryParseHubMessage(string sseMessage)
    {
        if (!sseMessage.StartsWith("data: ", StringComparison.Ordinal))
        {
            return null;
        }

        var json = sseMessage["data: ".Length..].Trim();
        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(json, _arcOptions.Value.JsonSerializerOptions);
    }
}
