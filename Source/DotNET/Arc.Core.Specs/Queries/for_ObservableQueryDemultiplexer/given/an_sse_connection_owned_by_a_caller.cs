// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// Opens a real SSE connection owned by one caller, so a spec can issue the subscribe and unsubscribe POSTs on it
/// as that caller or as somebody else and observe which the demultiplexer honors.
/// </summary>
public class an_sse_connection_owned_by_a_caller : an_observable_query_demultiplexer
{
    protected const string QueryName = "MyApp.Queries.SomeQuery";
    protected const string QueryId = "query-1";

    protected ClaimsPrincipal _owner;
    protected ClaimsPrincipal _anotherCaller;
    protected ClaimsPrincipal _ownerAfterATokenRefresh;

    protected IHttpRequestContext _connectionContext;
    protected ConcurrentQueue<string> _messages;
    protected CancellationTokenSource _connectionCancellation;
    protected BehaviorSubject<IEnumerable<string>> _subject;
    protected string _connectionId;
    protected int _statusCode;

    void Establish()
    {
        _messages = [];
        _connectionId = string.Empty;
        _statusCode = 0;
        _connectionCancellation = new CancellationTokenSource();

        _owner = PrincipalFor("owner-identity-id", "the-owner");
        _anotherCaller = PrincipalFor("another-identity-id", "another-caller");

        // Same identity, a different display name — what a connection held open across a token refresh looks like.
        _ownerAfterATokenRefresh = PrincipalFor("owner-identity-id", "the-owner-renamed");

        _subject = new BehaviorSubject<IEnumerable<string>>([]);
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
        _connectionContext.User.Returns(_owner);
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _messages.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });
    }

    protected static ClaimsPrincipal PrincipalFor(string identityId, string name) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, identityId), new Claim(ClaimTypes.Name, name)],
            "test"));

    /// <summary>
    /// Holds the connection open for the duration of <paramref name="script"/>, then disconnects the client.
    /// </summary>
    /// <param name="script">The control requests to run against the live connection.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task RunConnection(Func<Task> script)
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            await script();
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    protected IHttpRequestContext SubscribeContextFor(ClaimsPrincipal principal)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestAborted.Returns(CancellationToken.None);
        context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        context.User.Returns(principal);
        context.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId,
                QueryId,
                new ObservableQuerySubscriptionRequest(QueryName))));
        context.When(_ => _.SetStatusCode(Arg.Any<int>())).Do(callInfo => _statusCode = callInfo.Arg<int>());
        return context;
    }

    protected IHttpRequestContext UnsubscribeContextFor(ClaimsPrincipal principal)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestAborted.Returns(CancellationToken.None);
        context.User.Returns(principal);
        context.ReadBodyAsJson(typeof(ObservableQuerySSEUnsubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSEUnsubscribeRequest(_connectionId, QueryId)));
        context.When(_ => _.SetStatusCode(Arg.Any<int>())).Do(callInfo => _statusCode = callInfo.Arg<int>());
        return context;
    }

    protected int CountQueryResults() =>
        _messages
            .Select(TryParseHubMessage)
            .Count(_ => _ is not null && _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == QueryId);

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
