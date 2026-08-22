// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_get_context_is_disposed_while_query_is_performed : given.a_guarded_connection
{
    readonly ClaimsPrincipal _postPrincipal = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "fresh-caller")], "test"));
    readonly ConcurrentQueue<string> _messages = [];
    readonly TaskCompletionSource<QueryResult> _performCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _performStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _connectionCancellation = new();
    IHttpRequestContext _authorizationContext;
    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    string _connectionId;
    bool _getContextDisposed;
    int _getUserReads;
    int _getUserWrites;
    int _subscribeStatusCode;

    void Establish()
    {
        _connectionId = string.Empty;

        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                _authorizationContext = _httpRequestContextAccessor.Current;
                _performStarted.TrySetResult();
                return _performCompletion.Task;
            });

        _connectionContext = Substitute.For<IHttpRequestContext>();
        _connectionContext.RequestAborted.Returns(_connectionCancellation.Token);
        _connectionContext.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _connectionContext.User.Returns(_ =>
        {
            _getUserReads++;
            ObjectDisposedException.ThrowIf(_getContextDisposed, _connectionContext);

            return new ClaimsPrincipal();
        });
        _connectionContext.When(_ => _.User = Arg.Any<ClaimsPrincipal>())
            .Do(_ =>
            {
                _getUserWrites++;
                ObjectDisposedException.ThrowIf(_getContextDisposed, _connectionContext);
            });
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _messages.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });

        _subscribeContext = Substitute.For<IHttpRequestContext>();
        _subscribeContext.Headers.Returns(new Dictionary<string, string>());
        _subscribeContext.RequestAborted.Returns(CancellationToken.None);
        _subscribeContext.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _subscribeContext.User.Returns(_postPrincipal);
        _subscribeContext.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId,
                FirstQueryId,
                new ObservableQuerySubscriptionRequest(QueryName))));
        _subscribeContext.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(callInfo => _subscribeStatusCode = callInfo.Arg<int>());
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            var subscribeTask = _hub.HandleSSESubscribe(_subscribeContext);
            await _performStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            _getContextDisposed = true;
            var queryResult = QueryResult.Success(CorrelationId.New());
            queryResult.Data = _subject;
            _performCompletion.TrySetResult(queryResult);

            await subscribeTask;
            _subject.OnNext(["event-store-a"]);
            await WaitFor(() => _guardCalls.Count == 1);
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_complete_the_subscribe() => _subscribeStatusCode.ShouldEqual(200);
    [Fact] void should_authorize_with_the_post_context() => _authorizationContext.ShouldEqual(_subscribeContext);
    [Fact] void should_not_read_user_from_the_get_context() => _getUserReads.ShouldEqual(0);
    [Fact] void should_not_write_user_to_the_get_context() => _getUserWrites.ShouldEqual(0);
    [Fact] void should_consult_the_emission_guard() => _guardCalls.Count.ShouldEqual(1);
    [Fact] void should_give_the_emission_guard_the_post_principal() => _guardCalls.Single().Principal.ShouldEqual(_postPrincipal);

    bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;

        foreach (var hubMessage in _messages.Select(TryParseHubMessage).Where(_ => _ is not null).Select(_ => _!))
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
