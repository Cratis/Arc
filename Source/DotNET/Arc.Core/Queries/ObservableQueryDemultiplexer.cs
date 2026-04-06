// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using Cratis.Strings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IObservableQueryDemultiplexer"/> providing composite observable
/// query streaming over a fixed WebSocket endpoint (<c>/.cratis/queries/ws</c>) and a fixed
/// Server-Sent Events endpoint (<c>/.cratis/queries/sse</c>).
/// </summary>
/// <remarks>
/// <para>
/// Authorization is honored for every subscription through the query pipeline filters. If the
/// current user is not authorized to perform a query, an
/// <see cref="ObservableQueryHubMessageType.Unauthorized"/> message is sent instead of data.
/// </para>
/// <para>
/// Both transports support multiple concurrent subscriptions over a single connection.
/// The WebSocket transport uses bidirectional frames; the SSE transport uses separate POST
/// endpoints for subscribe/unsubscribe, correlated via a server-assigned connection identifier.
/// </para>
/// </remarks>
/// <param name="queryPipeline">The <see cref="IQueryPipeline"/> used to perform and authorize queries.</param>
/// <param name="queryContextManager">The <see cref="IQueryContextManager"/> for managing query contexts.</param>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> used to propagate the caller's identity into the authorization pipeline.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/> used to cancel connections on shutdown.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/> used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class ObservableQueryDemultiplexer(
    IQueryPipeline queryPipeline,
    IQueryContextManager queryContextManager,
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IHostApplicationLifetime hostApplicationLifetime,
    IOptions<ArcOptions> arcOptions,
    ILogger<ObservableQueryDemultiplexer> logger) : IObservableQueryDemultiplexer
{
    const int WebSocketBufferSize = 1024 * 4;

    readonly ConcurrentDictionary<string, SSEConnectionState> _sseConnections = new();
    readonly ChangeSetComputor _changeSetComputor = new(arcOptions.Value.JsonSerializerOptions);

    /// <inheritdoc/>
    public async Task HandleWebSocketConnection(IHttpRequestContext context)
    {
        logger.WebSocketClientConnected();

        httpRequestContextAccessor.Current = context;

        var webSocket = await context.WebSockets.AcceptWebSocket(context.RequestAborted);
        var subscriptions = new ConcurrentDictionary<string, IDisposable>();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            hostApplicationLifetime.ApplicationStopping);

        var keepAliveTracker = new KeepAliveTracker();
        var keepAliveTask = Task.CompletedTask;

        try
        {
#pragma warning disable CA2025 // keepAliveTask is always awaited in the finally block before linkedCts is disposed
            keepAliveTask = RunWebSocketKeepAlive(webSocket, keepAliveTracker, linkedCts.Token);
#pragma warning restore CA2025
            await ReadWebSocketMessages(webSocket, subscriptions, context, keepAliveTracker, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
        catch (WebSocketException ex)
        {
            logger.ErrorProcessingMessage(ex);
        }
        finally
        {
            await linkedCts.CancelAsync();
            await keepAliveTask;

            foreach (var subscription in subscriptions.Values)
            {
                subscription.Dispose();
            }

            logger.WebSocketClientDisconnected();
        }
    }

    /// <inheritdoc/>
    public async Task HandleSSEConnection(IHttpRequestContext context)
    {
        var connectionId = Guid.NewGuid().ToString();

        logger.SseClientConnected(connectionId);

        httpRequestContextAccessor.Current = context;

        context.SetSseResponseHeaders();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            hostApplicationLifetime.ApplicationStopping);

        var state = new SSEConnectionState(context, linkedCts);
        _sseConnections[connectionId] = state;

        var keepAliveTask = RunSseKeepAlive(context, state.KeepAliveTracker, linkedCts);

        try
        {
            // Send the Connected message so the client knows its connection ID for POST requests.
            await SendSseMessage(context, ObservableQueryHubMessage.CreateConnected(connectionId), state.KeepAliveTracker, linkedCts);

            // Block until the client disconnects or the server shuts down.
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            linkedCts.Token.Register(() => tcs.TrySetResult());
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
        finally
        {
            _sseConnections.TryRemove(connectionId, out _);

            foreach (var subscription in state.Subscriptions.Values)
            {
                subscription.Dispose();
            }

            state.Subscriptions.Clear();

            await linkedCts.CancelAsync();
            await keepAliveTask;
        }

        logger.SseClientDisconnected(connectionId);
    }

    /// <inheritdoc/>
    public async Task HandleSSESubscribe(IHttpRequestContext context)
    {
        if (await context.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), context.RequestAborted)
            is not ObservableQuerySSESubscribeRequest body
            || string.IsNullOrEmpty(body.ConnectionId)
            || string.IsNullOrEmpty(body.QueryId)
            || body.Request is null)
        {
            context.SetStatusCode(400);
            return;
        }

        if (!_sseConnections.TryGetValue(body.ConnectionId, out var state))
        {
            logger.SseUnknownConnection(body.ConnectionId);
            context.SetStatusCode(404);
            return;
        }

        // The subscribe POST carries the latest cookies and headers, which the middleware
        // already authenticated. Transfer the principal to the SSE connection context so the
        // authorization pipeline sees the current identity — not the one from when the SSE
        // GET was originally established (which may have been anonymous).
        state.Context.User = context.User;
        httpRequestContextAccessor.Current = state.Context;

        logger.ClientSubscribed(body.Request.QueryName, body.QueryId);

        // If there's an existing subscription for this queryId, dispose it first
        if (state.Subscriptions.TryRemove(body.QueryId, out var existing))
        {
            existing.Dispose();
        }

        var wasUnauthorized = false;

        var subscription = await CreateSubscription(
            state.Context,
            body.QueryId,
            body.Request,
            async result =>
            {
                var msg = ObservableQueryHubMessage.CreateQueryResult(body.QueryId, result);
                await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource);
            },
            async (id, errorMsg) =>
            {
                var msg = ObservableQueryHubMessage.CreateError(id, errorMsg);
                await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource);
            },
            async id =>
            {
                wasUnauthorized = true;
                var msg = ObservableQueryHubMessage.CreateUnauthorized(id);
                await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource);
            },
            state.CancellationTokenSource.Token);

        if (wasUnauthorized)
        {
            context.SetStatusCode(401);
            return;
        }

        if (subscription is not null)
        {
            state.Subscriptions[body.QueryId] = subscription;
        }

        context.SetStatusCode(200);
    }

    /// <inheritdoc/>
    public async Task HandleSSEUnsubscribe(IHttpRequestContext context)
    {
        if (await context.ReadBodyAsJson(typeof(ObservableQuerySSEUnsubscribeRequest), context.RequestAborted)
            is not ObservableQuerySSEUnsubscribeRequest body
            || string.IsNullOrEmpty(body.ConnectionId)
            || string.IsNullOrEmpty(body.QueryId))
        {
            context.SetStatusCode(400);
            return;
        }

        if (!_sseConnections.TryGetValue(body.ConnectionId, out var state))
        {
            logger.SseUnknownConnection(body.ConnectionId);
            context.SetStatusCode(404);
            return;
        }

        if (state.Subscriptions.TryRemove(body.QueryId, out var subscription))
        {
            subscription.Dispose();
            logger.ClientUnsubscribed(body.QueryId);
        }

        context.SetStatusCode(200);
    }

    async Task ReadWebSocketMessages(
        IWebSocket webSocket,
        ConcurrentDictionary<string, IDisposable> subscriptions,
        IHttpRequestContext context,
        KeepAliveTracker keepAliveTracker,
        CancellationToken token)
    {
        var buffer = new byte[WebSocketBufferSize];

        while (!token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult received;

            try
            {
                received = await webSocket.Receive(new ArraySegment<byte>(buffer), token);
            }
            catch (WebSocketException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (received.CloseStatus.HasValue)
            {
                await webSocket.Close(received.CloseStatus.Value, received.CloseStatusDescription, token);
                break;
            }

            if (received.MessageType != System.Net.WebSockets.WebSocketMessageType.Text)
            {
                continue;
            }

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(buffer, 0, received.Count);
                var message = JsonSerializer.Deserialize<ObservableQueryHubMessage>(json, arcOptions.Value.JsonSerializerOptions);

                if (message is null)
                {
                    continue;
                }

                await ProcessWebSocketMessage(message, webSocket, subscriptions, context, keepAliveTracker, token);
            }
            catch (Exception ex)
            {
                logger.ErrorProcessingMessage(ex);
            }
        }

        // Clean up subscriptions on disconnect
        foreach (var subscription in subscriptions.Values)
        {
            subscription.Dispose();
        }

        subscriptions.Clear();
    }

    async Task ProcessWebSocketMessage(
        ObservableQueryHubMessage message,
        IWebSocket webSocket,
        ConcurrentDictionary<string, IDisposable> subscriptions,
        IHttpRequestContext context,
        KeepAliveTracker keepAliveTracker,
        CancellationToken token)
    {
        // Any inbound message from the client counts as activity — no keep-alive needed.
        keepAliveTracker.RecordActivity();

        switch (message.Type)
        {
            case ObservableQueryHubMessageType.Subscribe:
                await HandleWebSocketSubscribe(message, webSocket, subscriptions, context, keepAliveTracker, token);
                break;

            case ObservableQueryHubMessageType.Unsubscribe:
                HandleWebSocketUnsubscribe(message, subscriptions);
                break;

            case ObservableQueryHubMessageType.Ping:
                await SendWebSocketMessage(webSocket, ObservableQueryHubMessage.CreatePong(message.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), keepAliveTracker, token);
                break;
        }
    }

    async Task HandleWebSocketSubscribe(
        ObservableQueryHubMessage message,
        IWebSocket webSocket,
        ConcurrentDictionary<string, IDisposable> subscriptions,
        IHttpRequestContext context,
        KeepAliveTracker keepAliveTracker,
        CancellationToken token)
    {
        var request = DeserializeSubscriptionRequest(message.Payload);
        if (request is null || string.IsNullOrEmpty(request.QueryName))
        {
            logger.MissingQueryName(message.QueryId);
            return;
        }

        logger.ClientSubscribed(request.QueryName, message.QueryId ?? string.Empty);

        // If there's an existing subscription for this queryId, dispose it first
        if (message.QueryId is not null && subscriptions.TryRemove(message.QueryId, out var existing))
        {
            existing.Dispose();
        }

        var queryId = message.QueryId ?? Guid.NewGuid().ToString();

        var subscription = await CreateSubscription(
            context,
            queryId,
            request,
            async result =>
            {
                var msg = ObservableQueryHubMessage.CreateQueryResult(queryId, result);
                await SendWebSocketMessage(webSocket, msg, keepAliveTracker, token);
            },
            async (id, errorMsg) =>
            {
                var msg = ObservableQueryHubMessage.CreateError(id, errorMsg);
                await SendWebSocketMessage(webSocket, msg, keepAliveTracker, token);
            },
            async id =>
            {
                var msg = ObservableQueryHubMessage.CreateUnauthorized(id);
                await SendWebSocketMessage(webSocket, msg, keepAliveTracker, token);
            },
            token);

        if (subscription is not null)
        {
            subscriptions[queryId] = subscription;
        }
    }

    void HandleWebSocketUnsubscribe(
        ObservableQueryHubMessage message,
        ConcurrentDictionary<string, IDisposable> subscriptions)
    {
        var queryId = message.QueryId;
        if (queryId is null)
        {
            return;
        }

        if (subscriptions.TryRemove(queryId, out var subscription))
        {
            subscription.Dispose();
            logger.ClientUnsubscribed(queryId);
        }
    }

    async Task<IDisposable?> CreateSubscription(
        IHttpRequestContext context,
        string queryId,
        ObservableQuerySubscriptionRequest request,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        Func<string, Task> onUnauthorized,
        CancellationToken token)
    {
        var paging = BuildPaging(request);
        var sorting = BuildSorting(request);
        var arguments = BuildQueryArguments(request.Arguments);
        var fullyQualifiedName = new FullyQualifiedQueryName(request.QueryName);

        // Run through the full query pipeline (including authorization filters)
        var queryResult = await queryPipeline.Perform(
            fullyQualifiedName,
            arguments,
            paging,
            sorting,
            context.RequestServices);

        if (!queryResult.IsAuthorized)
        {
            logger.QueryUnauthorized(request.QueryName, queryId);
            await onUnauthorized(queryId);
            return null;
        }

        if (!queryResult.IsSuccess)
        {
            var errorMsg = string.Join("; ", queryResult.ExceptionMessages);
            await onError(queryId, errorMsg);
            return null;
        }

        var streamingData = queryResult.Data;
        if (streamingData is null || !IsStreamingResult(streamingData))
        {
            // Non-streaming result — send current snapshot and return null (no long-lived subscription)
            var queryResultWithData = new QueryResult
            {
                Data = queryResult.Data,
                IsAuthorized = true,
                ValidationResults = [],
                ExceptionMessages = [],
                ExceptionStackTrace = string.Empty,
                Paging = queryResult.Paging
            };

            await onNext(queryResultWithData);
            return null;
        }

        return SubscribeToStreamingData(streamingData, queryId, queryResult.Paging, onNext, onError, token);
    }

    IDisposable? SubscribeToStreamingData(
        object streamingData,
        string queryId,
        PagingInfo paging,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        CancellationToken token)
    {
        var type = streamingData.GetType();

        if (type.ImplementsOpenGeneric(typeof(ISubject<>)))
        {
            return SubscribeToSubject(streamingData, type, queryId, paging, onNext, onError, token);
        }

        if (type.ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)))
        {
            _ = StreamAsyncEnumerable(streamingData, type, queryId, paging, onNext, onError, token);
        }

        return null;
    }

    IDisposable SubscribeToSubject(
        object subject,
        Type subjectType,
        string queryId,
        PagingInfo paging,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        CancellationToken token)
    {
        var elementType = subjectType.GetInterfaces()
            .First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(ISubject<>))
            .GetGenericArguments()[0];

        var method = typeof(ObservableQueryDemultiplexer)
            .GetMethod(nameof(SubscribeToSubjectOfType), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        return (IDisposable)method.Invoke(this, [subject, queryId, paging, onNext, onError, token])!;
    }

    IDisposable SubscribeToSubjectOfType<T>(
        ISubject<T> subject,
        string queryId,
        PagingInfo paging,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        CancellationToken token)
    {
        IEnumerable<object>? previousItems = null;

        return subject.Subscribe(
            data =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                // Compute ChangeSet for enumerable results (excludes single-item and string results).
                ChangeSet? changeSet = null;
                if (data is IEnumerable enumerable && data is not string)
                {
                    var currentItems = enumerable.Cast<object>().ToArray();
                    changeSet = _changeSetComputor.Compute(previousItems, currentItems);
                    previousItems = currentItems;
                }

                var queryContext = queryContextManager.Current;
                var result = new QueryResult
                {
                    Data = data!,
                    IsAuthorized = true,
                    ValidationResults = [],
                    ExceptionMessages = [],
                    ExceptionStackTrace = string.Empty,
                    Paging = paging,
                    ChangeSet = changeSet
                };

                if (queryContext is not null)
                {
                    result.Paging = new PagingInfo(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems);
                }

                _ = onNext(result);
            },
            error =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                // Transport errors (broken pipe, reset connection) indicate the client
                // disconnected — cancel the connection and do not forward to the client.
                if (error is IOException)
                {
                    return;
                }

                logger.SubscriptionError(queryId, error);
                _ = onError(queryId, error.Message);
            });
    }

    async Task StreamAsyncEnumerable(
        object enumerable,
        Type enumerableType,
        string queryId,
        PagingInfo paging,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        CancellationToken token)
    {
        var elementType = enumerableType.GetInterfaces()
            .First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            .GetGenericArguments()[0];

        var method = typeof(ObservableQueryDemultiplexer)
            .GetMethod(nameof(StreamAsyncEnumerableOfType), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        await (Task)method.Invoke(this, [enumerable, queryId, paging, onNext, onError, token])!;
    }

    async Task StreamAsyncEnumerableOfType<T>(
        IAsyncEnumerable<T> enumerable,
        string queryId,
        PagingInfo paging,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        CancellationToken token)
    {
        try
        {
            await foreach (var item in enumerable.WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var result = new QueryResult
                {
                    Data = item!,
                    IsAuthorized = true,
                    ValidationResults = [],
                    ExceptionMessages = [],
                    ExceptionStackTrace = string.Empty,
                    Paging = paging
                };

                await onNext(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected.
        }
        catch (IOException)
        {
            // Transport error — client disconnected. Do not forward to the client.
        }
        catch (Exception ex)
        {
            logger.SubscriptionError(queryId, ex);
            await onError(queryId, ex.Message);
        }
    }

    async Task SendWebSocketMessage(IWebSocket webSocket, ObservableQueryHubMessage message, KeepAliveTracker keepAliveTracker, CancellationToken token)
    {
        try
        {
            if (webSocket.State != WebSocketState.Open)
            {
                return;
            }

            var json = JsonSerializer.SerializeToUtf8Bytes(message, arcOptions.Value.JsonSerializerOptions);
            await webSocket.Send(new ArraySegment<byte>(json), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            keepAliveTracker.RecordMessageSent();
        }
        catch (Exception ex)
        {
            logger.ErrorSendingMessage(ex);
        }
    }

    async Task SendSseMessage(IHttpRequestContext context, ObservableQueryHubMessage message, KeepAliveTracker keepAliveTracker, CancellationTokenSource cts)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, arcOptions.Value.JsonSerializerOptions);
            await context.Write($"data: {json}\n\n", cts.Token);
            keepAliveTracker.RecordMessageSent();
        }
        catch (HttpListenerException)
        {
            // Client disconnected — cancel the connection token source to trigger cleanup.
            await cts.CancelAsync();
        }
        catch (IOException)
        {
            // On macOS and some .NET runtimes, HttpListener throws IOException for broken-pipe
            // instead of HttpListenerException — treat identically.
            await cts.CancelAsync();
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
        catch (Exception ex)
        {
            logger.ErrorSendingMessage(ex);
        }
    }

    async Task RunWebSocketKeepAlive(IWebSocket webSocket, KeepAliveTracker keepAliveTracker, CancellationToken token)
    {
        var interval = arcOptions.Value.Query.KeepAliveInterval;

        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(interval, token);

                if (!token.IsCancellationRequested && keepAliveTracker.ShouldSendKeepAlive(interval))
                {
                    await SendWebSocketMessage(webSocket, ObservableQueryHubMessage.CreatePing(), keepAliveTracker, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
    }

    async Task RunSseKeepAlive(IHttpRequestContext context, KeepAliveTracker keepAliveTracker, CancellationTokenSource cts)
    {
        var interval = arcOptions.Value.Query.KeepAliveInterval;

        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(interval, cts.Token);

                if (!cts.Token.IsCancellationRequested && keepAliveTracker.ShouldSendKeepAlive(interval))
                {
                    await SendSseMessage(context, ObservableQueryHubMessage.CreatePing(), keepAliveTracker, cts);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
    }

#pragma warning disable SA1204 // Static members should appear before non-static members
    static bool IsStreamingResult(object data) =>
        data.GetType().ImplementsOpenGeneric(typeof(ISubject<>)) ||
        data.GetType().ImplementsOpenGeneric(typeof(IAsyncEnumerable<>));

    static Paging BuildPaging(ObservableQuerySubscriptionRequest request)
    {
        if (request.PageSize.HasValue)
        {
            return new Paging(request.Page ?? 0, request.PageSize.Value, true);
        }

        return Paging.NotPaged;
    }

    static Sorting BuildSorting(ObservableQuerySubscriptionRequest request)
    {
        if (!string.IsNullOrEmpty(request.SortBy) && !string.IsNullOrEmpty(request.SortDirection))
        {
            var direction = request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Descending
                : SortDirection.Ascending;

            return new Sorting(request.SortBy.ToPascalCase(), direction);
        }

        return Sorting.None;
    }

    static QueryArguments BuildQueryArguments(IDictionary<string, string?>? arguments)
    {
        if (arguments is null)
        {
            return QueryArguments.Empty;
        }

        var result = new QueryArguments();
        foreach (var kvp in arguments)
        {
            if (kvp.Value is not null)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    ObservableQuerySubscriptionRequest? DeserializeSubscriptionRequest(object? payload)
    {
        if (payload is null)
        {
            return null;
        }

        if (payload is ObservableQuerySubscriptionRequest request)
        {
            return request;
        }

        // When deserialized from JSON, the payload will be a JsonElement
        if (payload is JsonElement element)
        {
            return element.Deserialize<ObservableQuerySubscriptionRequest>(arcOptions.Value.JsonSerializerOptions);
        }

        try
        {
            var json = JsonSerializer.Serialize(payload, arcOptions.Value.JsonSerializerOptions);
            return JsonSerializer.Deserialize<ObservableQuerySubscriptionRequest>(json, arcOptions.Value.JsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }
#pragma warning restore SA1204

    sealed record SSEConnectionState(
        IHttpRequestContext Context,
        CancellationTokenSource CancellationTokenSource)
    {
        public ConcurrentDictionary<string, IDisposable> Subscriptions { get; } = new();
        public KeepAliveTracker KeepAliveTracker { get; } = new();
    }
}
