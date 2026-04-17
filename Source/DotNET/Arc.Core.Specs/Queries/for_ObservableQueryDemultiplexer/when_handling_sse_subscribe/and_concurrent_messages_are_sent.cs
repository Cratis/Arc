// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// Tests that concurrent SSE message writes (query results + keep-alive pings) do not
/// cause race conditions or exceptions. This guards against ArgumentNullException("array")
/// that can occur during concurrent writes to response pipes.
/// </summary>
public class and_concurrent_messages_are_sent : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-concurrent";
    const int NumberOfEmissions = 50;

    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    CancellationTokenSource _connectionCancellation;
    ConcurrentQueue<string> _messages;
    Subject<IEnumerable<string>> _subject;
    string _connectionId;
    int _subscribeStatusCode;
    Exception? _caughtException;

    void Establish()
    {
        _connectionCancellation = new CancellationTokenSource();
        _messages = [];
        _connectionId = string.Empty;
        _caughtException = null;

        _subject = new Subject<IEnumerable<string>>();
        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                var queryResult = QueryResult.Success(CorrelationId.New());
                queryResult.Data = _subject;
                return Task.FromResult(queryResult);
            });

        _connectionContext = Substitute.For<IHttpRequestContext>();
        _connectionContext.RequestAborted.Returns(_connectionCancellation.Token);
        _connectionContext.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                try
                {
                    _messages.Enqueue(callInfo.Arg<string>());
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _caughtException = ex;
                    throw;
                }
            });

        _subscribeContext = Substitute.For<IHttpRequestContext>();
        _subscribeContext.RequestAborted.Returns(CancellationToken.None);
        _subscribeContext.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId,
                QueryId,
                new ObservableQuerySubscriptionRequest(ControllerQueryName))));
        _subscribeContext.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(callInfo => _subscribeStatusCode = callInfo.Arg<int>());
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);

        // Wait for connection message to be sent (contains connection ID)
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        // Subscribe to the query
        await _hub.HandleSSESubscribe(_subscribeContext);

        // Emit multiple results in rapid succession to create concurrent write pressure
        // with the keep-alive pings that may be running in the background.
        for (var i = 0; i < NumberOfEmissions; i++)
        {
            _subject.OnNext([$"event-{i}"]);
            await Task.Delay(5);
        }

        // Wait for at least some query result messages to arrive
        await WaitFor(HasMultipleQueryResultMessages);

        // Cleanup
        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    [Fact]
    void should_return_200_from_subscribe() =>
        _subscribeStatusCode.ShouldEqual(200);

    [Fact]
    void should_not_throw_exception_during_concurrent_writes() =>
        _caughtException.ShouldBeNull();

    [Fact]
    void should_send_multiple_query_result_messages() =>
        HasMultipleQueryResultMessages().ShouldBeTrue();

    [Fact]
    void should_send_connected_message() =>
        TryExtractConnectionId(out _).ShouldBeTrue();

    bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;

        foreach (var hubMessage in _messages
                     .Select(TryParseHubMessage)
                     .Where(_ => _ is not null)
                     .Select(_ => _!))
        {
            if (hubMessage.Type != ObservableQueryHubMessageType.Connected ||
                hubMessage.Payload is not JsonElement payload)
            {
                continue;
            }

            connectionId = payload.GetString() ?? string.Empty;
            return !string.IsNullOrEmpty(connectionId);
        }

        return false;
    }

    bool HasMultipleQueryResultMessages()
    {
        var count = 0;

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

            if (TryGetPropertyIgnoreCase(payload, "data", out _))
            {
                count++;
            }
        }

        return count >= 5;
    }

    static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        var alternateCasing = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        return element.TryGetProperty(alternateCasing, out value);
    }

    ObservableQueryHubMessage? TryParseHubMessage(string sseMessage)
    {
        if (!sseMessage.StartsWith("data: ", StringComparison.Ordinal))
        {
            return null;
        }

        var json = sseMessage["data: ".Length..].Trim();
        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(
            json,
            _arcOptions.Value.JsonSerializerOptions);
    }
}
