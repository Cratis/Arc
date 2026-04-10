// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_subscription_is_unauthorized : given.an_observable_query_demultiplexer
{
    const string QueryName = "MyApp.Queries.SecuredQuery";
    const string QueryId = "query-secured";

    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    CancellationTokenSource _connectionCancellation;
    ConcurrentQueue<string> _messages;
    string _connectionId;
    int _subscribeStatusCode;

    void Establish()
    {
        _connectionCancellation = new CancellationTokenSource();
        _messages = [];
        _connectionId = string.Empty;

        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
            .Returns(_ => Task.FromResult(QueryResult.Unauthorized(CorrelationId.New())));

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
                new ObservableQuerySubscriptionRequest(QueryName))));
        _subscribeContext.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(callInfo => _subscribeStatusCode = callInfo.Arg<int>());
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);

        await WaitFor(() => TryExtractConnectionId(out _connectionId));
        await _hub.HandleSSESubscribe(_subscribeContext);

        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    [Fact] void should_return_401_from_subscribe() => _subscribeStatusCode.ShouldEqual(401);

    [Fact]
    void should_send_unauthorized_message_over_sse() => HasUnauthorizedMessage().ShouldBeTrue();

    bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;
        foreach (var hubMessage in _messages
                     .Select(TryParseHubMessage)
                     .Where(m => m is not null)
                     .Select(m => m!))
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

    bool HasUnauthorizedMessage()
    {
        foreach (var hubMessage in _messages
                     .Select(TryParseHubMessage)
                     .Where(m => m is not null)
                     .Select(m => m!))
        {
            if (hubMessage.Type == ObservableQueryHubMessageType.Unauthorized &&
                hubMessage.QueryId == QueryId)
            {
                return true;
            }
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

    static async Task WaitFor(Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(2);
        while (DateTimeOffset.UtcNow < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }
    }
}
