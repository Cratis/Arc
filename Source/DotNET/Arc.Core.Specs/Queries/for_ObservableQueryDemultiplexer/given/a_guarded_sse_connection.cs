// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// Drives a full SSE connection: every query id in <see cref="a_guarded_connection._queryIds"/> is subscribed through
/// its own POST, the spec's script then runs against the live subscriptions, and the client disconnects.
/// </summary>
public class a_guarded_sse_connection : a_guarded_connection
{
    protected IHttpRequestContext _connectionContext;
    protected ConcurrentQueue<string> _messages;
    protected ConcurrentDictionary<string, int> _subscribeStatusCodes;

    protected CancellationTokenSource _connectionCancellation;
    protected string _connectionId;

    void Establish()
    {
        _messages = [];
        _subscribeStatusCodes = new();
        _connectionId = string.Empty;
        _connectionCancellation = new CancellationTokenSource();

        _connectionContext = Substitute.For<IHttpRequestContext>();
        _connectionContext.RequestAborted.Returns(_connectionCancellation.Token);
        _connectionContext.RequestServices.Returns(Substitute.For<IServiceProvider>());

        // The GET that opens the stream is authenticated exactly as the control POSTs that follow it are, and the
        // demultiplexer only accepts a control request from the caller that opened the connection.
        _connectionContext.User.Returns(_principal);
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _messages.Enqueue(callInfo.Arg<string>());
                _signals.Signal();
                return Task.CompletedTask;
            });
    }

    protected async Task RunConnection(Func<Task> script)
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        foreach (var queryId in _queryIds)
        {
            await _hub.HandleSSESubscribe(CreateSubscribeContext(queryId));
        }

        await script();

        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    protected async Task Unsubscribe(string queryId)
    {
        var unsubscribeContext = Substitute.For<IHttpRequestContext>();
        unsubscribeContext.RequestAborted.Returns(CancellationToken.None);
        unsubscribeContext.User.Returns(_principal);
        unsubscribeContext.ReadBodyAsJson(typeof(ObservableQuerySSEUnsubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ObservableQuerySSEUnsubscribeRequest(_connectionId, queryId)));

        await _hub.HandleSSEUnsubscribe(unsubscribeContext);
    }

    protected int CountQueryResultsFor(string queryId) =>
        HubMessages.Count(_ => _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == queryId);

    protected bool HasUnauthorizedFor(string queryId) =>
        HubMessages.Any(_ => _.Type == ObservableQueryHubMessageType.Unauthorized && _.QueryId == queryId);

    protected bool HasErrorFor(string queryId) =>
        HubMessages.Any(_ => _.Type == ObservableQueryHubMessageType.Error && _.QueryId == queryId);

    protected List<QueryResult> QueryResultsFor(string queryId) =>
        [.. HubMessages
            .Where(_ => _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == queryId && _.Payload is JsonElement)
            .Select(_ => JsonSerializer.Deserialize<QueryResult>(((JsonElement)_.Payload!).GetRawText(), _arcOptions.Value.JsonSerializerOptions))
            .Where(_ => _ is not null)
            .Select(_ => _!)];

    protected IEnumerable<ObservableQueryHubMessage> HubMessages =>
        _messages.Select(TryParseHubMessage).Where(_ => _ is not null).Select(_ => _!);

    protected IHttpRequestContext CreateSubscribeContext(string queryId, long? revision = null)
    {
        var subscribeContext = Substitute.For<IHttpRequestContext>();
        subscribeContext.RequestAborted.Returns(CancellationToken.None);
        subscribeContext.RequestServices.Returns(_admissionServices);

        // The subscribe POST carries the freshest identity; the demultiplexer transfers it onto the SSE connection.
        ConfigureControlRequest(subscribeContext, CreateRequest, queryId);
        return subscribeContext;

        ObservableQuerySSESubscribeRequest CreateRequest()
        {
            var request = new ObservableQuerySSESubscribeRequest(
                _connectionId,
                queryId,
                new ObservableQuerySubscriptionRequest(QueryName, RawArguments));
            typeof(ObservableQuerySSESubscribeRequest).GetProperty("Revision")!.SetValue(request, revision);
            return request;
        }
    }

    protected IHttpRequestContext CreateUnsubscribeContext(string queryId, long? revision = null)
    {
        var unsubscribeContext = Substitute.For<IHttpRequestContext>();
        unsubscribeContext.RequestAborted.Returns(CancellationToken.None);
        ConfigureControlRequest(unsubscribeContext, CreateRequest, queryId);
        return unsubscribeContext;

        ObservableQuerySSEUnsubscribeRequest CreateRequest()
        {
            var request = new ObservableQuerySSEUnsubscribeRequest(_connectionId, queryId);
            typeof(ObservableQuerySSEUnsubscribeRequest).GetProperty("Revision")!.SetValue(request, revision);
            return request;
        }
    }

    void ConfigureControlRequest<TRequest>(IHttpRequestContext context, Func<TRequest> createRequest, string queryId)
    {
        context.User.Returns(_principal);
        context.ReadBodyAsJson(typeof(TRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(createRequest()));
        context.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(callInfo => _subscribeStatusCodes[queryId] = callInfo.Arg<int>());
    }

    protected bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;

        foreach (var hubMessage in HubMessages)
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
