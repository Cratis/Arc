// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// Reproduces the production-crashing race where an observable-query emission is in flight (interception has
/// not yet completed) at the moment the SSE connection ends and its per-subscription resources — the emission
/// gate (a <see cref="SemaphoreSlim"/>) and the linked <see cref="CancellationTokenSource"/> — are disposed.
/// When the emission then resumes it touches those disposed objects; because the emission callback is
/// <c>async void</c>, the resulting <see cref="ObjectDisposedException"/> would previously go unhandled on a
/// background thread and terminate the whole process. The emission must instead be handled gracefully.
/// </summary>
public class and_emission_arrives_after_connection_is_disposed : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";

    IQueryHealthTracker _observableHealthTracker;
    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    CancellationTokenSource _connectionCancellation;
    ConcurrentQueue<string> _messages;
    BehaviorSubject<IEnumerable<string>> _subject;
    TaskCompletionSource<IEnumerable<object>> _interceptionGate;
    TaskCompletionSource _dataServed;
    string _connectionId;
    bool _connectionCompleted;
    bool _lateEmissionStopped;
    Exception _thrownWhenReleasingInFlightEmission;

    void Establish()
    {
        _connectionCancellation = new CancellationTokenSource();
        _messages = [];
        _connectionId = string.Empty;
        _interceptionGate = new TaskCompletionSource<IEnumerable<object>>();
        _dataServed = new TaskCompletionSource();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Rebuild the hub with a health tracker we can observe. A late emission stopped by teardown must not be
        // recorded as data served, because no result reaches the disconnected client.
        _observableHealthTracker = Substitute.For<IQueryHealthTracker>();
        _observableHealthTracker
            .When(_ => _.RecordDataServed(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => _dataServed.TrySetResult());

        _hub = new ObservableQueryDemultiplexer(
            _queryPipeline,
            _queryContextManager,
            _httpRequestContextAccessor,
            _hostApplicationLifetime,
            _readModelInterceptors,
            _serviceProvider,
            _arcOptions,
            _observableHealthTracker,
            _emissionGuards,
            _logger);

        // Hold interception open so the first emission stays in flight (holding the emission gate) while the
        // connection is torn down underneath it.
        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(_ => _interceptionGate.Task);

        _subject = new BehaviorSubject<IEnumerable<string>>([]);
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
                _messages.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });

        _subscribeContext = Substitute.For<IHttpRequestContext>();
        _subscribeContext.RequestAborted.Returns(CancellationToken.None);
        _subscribeContext.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId,
                QueryId,
                new ObservableQuerySubscriptionRequest(ControllerQueryName))));
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);

        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        // Subscribing to the BehaviorSubject immediately delivers its current value, so an emission is now in
        // flight and suspended inside interception, holding the emission gate.
        await _hub.HandleSSESubscribe(_subscribeContext);
        await WaitFor(() => _readModelInterceptors.ReceivedCalls().Any());

        // Tear the connection down. The finally disposes the subscription (and thus the emission gate) and,
        // on return, the linked cancellation source — all while the emission is still in flight.
        await _connectionCancellation.CancelAsync();
        await connectionTask;
        _connectionCompleted = true;

        // Let the in-flight emission resume against the now-disposed resources. Before the fix this crashed
        // the process; it must now observe its subscription cancellation and become a graceful no-op.
        try
        {
            var logCallsBeforeRelease = LogCallCount;
            _interceptionGate.SetResult(["item-a"]);
            await WaitForLogAfter(logCallsBeforeRelease);
            _lateEmissionStopped = true;
        }
        catch (Exception ex)
        {
            _thrownWhenReleasingInFlightEmission = ex;
        }
    }

    [Fact] void should_complete_the_connection_cleanly() => _connectionCompleted.ShouldBeTrue();
    [Fact] void should_not_throw_when_the_in_flight_emission_resumes() => _thrownWhenReleasingInFlightEmission.ShouldBeNull();
    [Fact] void should_stop_the_late_emission_gracefully() => _lateEmissionStopped.ShouldBeTrue();
    [Fact] void should_not_track_the_late_emission_as_data_served() => _dataServed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_write_a_query_result_to_the_disposed_connection() => HasQueryResultMessage().ShouldBeFalse();

    int LogCallCount => _logger.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(ILogger.Log));

    async Task WaitForLogAfter(int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (LogCallCount <= count)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    bool HasQueryResultMessage() =>
        _messages
            .Select(TryParseHubMessage)
            .Any(_ => _ is { Type: ObservableQueryHubMessageType.QueryResult } message && message.QueryId == QueryId);

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
