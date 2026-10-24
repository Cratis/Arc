// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Calendar;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Extensions.Time.Testing;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_clock_boundary_passes : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";

    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    CancellationTokenSource _connectionCancellation;
    ConcurrentQueue<string> _messages;
    static readonly DateTimeOffset _start = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    static readonly DateOnly _dueDate = new(2026, 5, 2);

    FakeTimeProvider _time;
    ISubject<IEnumerable<string>> _subject;
    string _connectionId;
    int _subscribeStatusCode;

    void Establish()
    {
        _connectionCancellation = new CancellationTokenSource();
        _messages = [];
        _connectionId = string.Empty;

        _time = new FakeTimeProvider(_start);
        var context = new ClockBoundaryContext(
            new TenantId("tenant"),
            TimeZoneInfo.Utc,
            _time,
            Substitute.For<ITenantScope>(),
            new ChangeSetComputor(_arcOptions.Value.JsonSerializerOptions),
            CancellationToken.None);
        _subject = new BehaviorSubject<string>("event-store-a").ReevaluateAtBoundaries(
            context,
            (item, reading) => new ClockEvaluation<IEnumerable<string>>(
                reading.Today >= _dueDate ? [item] : [],
                LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant)));
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
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
                _messages.Enqueue(callInfo.Arg<string>());
                _signals.Signal();
                return Task.CompletedTask;
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

        await WaitFor(() => TryExtractConnectionId(out _connectionId));
        await _hub.HandleSSESubscribe(_subscribeContext);
        await WaitFor(() => QueryResultCount() == 1);
        _time.Advance(TimeSpan.FromHours(13));
        await WaitFor(() => QueryResultCount() == 2);

        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    [Fact] void should_return_200_from_subscribe() => _subscribeStatusCode.ShouldEqual(200);

    [Fact]
    void should_call_query_pipeline_for_controller_fully_qualified_name() =>
        _queryPipeline.Received(1).Perform(
            Arg.Is<FullyQualifiedQueryName>(_ => _.Value == ControllerQueryName),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());

    [Fact] void should_send_the_clock_epoch_as_a_second_query_result_over_sse() => QueryResultCount().ShouldEqual(2);

    bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;

        foreach (var hubMessage in _messages
                     .Select(TryParseHubMessage)
                     .Where(_ => _ is not null)
                     .Select(_ => _))
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

    int QueryResultCount() => _messages
        .Select(TryParseHubMessage)
        .Count(hubMessage => hubMessage is not null &&
            hubMessage.Type == ObservableQueryHubMessageType.QueryResult &&
            hubMessage.QueryId == QueryId);

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
        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(json, _arcOptions.Value.JsonSerializerOptions);
    }
}
