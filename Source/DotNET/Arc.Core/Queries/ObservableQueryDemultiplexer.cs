// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Cratis.Reflection;
using Cratis.Strings;
using Microsoft.Extensions.DependencyInjection;
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
/// <para>
/// Each subject-backed subscription gets its own <see cref="IServiceScope"/>, created at subscribe time from
/// <paramref name="serviceProvider"/> and disposed when the subscription ends. Chronicle's client services are
/// registered scoped, resolving the current tenant's namespace the first time they are used within a scope and
/// caching it for the scope's lifetime — resolving them from the root container instead would cache whichever
/// tenant asked first and reuse it for every subscription thereafter. Emissions arrive on the subject's own
/// producer thread, where the AsyncLocal tenant context does not flow, so <see cref="IHttpRequestContextAccessor.Current"/>
/// is restored to the subscribing connection immediately before each interception call.
/// </para>
/// <para>
/// The subscription-time authorization verdict gates <em>obtaining</em> the stream, not the stream itself. An
/// application that needs the verdict re-checked while a subscription is running implements an
/// <see cref="IGuardObservableQueryEmission"/>; it is then consulted for every emission, and can withhold a single
/// emission or terminate the subscription. With no guard implemented nothing is dispatched and emissions take exactly
/// the path they always did.
/// </para>
/// </remarks>
/// <param name="queryPipeline">The <see cref="IQueryPipeline"/> used to perform and authorize queries.</param>
/// <param name="queryContextManager">The <see cref="IQueryContextManager"/> for managing query contexts.</param>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> used to propagate the caller's identity into the authorization pipeline, and restored around each emission so tenant resolution sees the subscribing connection.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/> used to cancel connections on shutdown.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> used to intercept (e.g. decrypt compliance/PII properties on) each emitted read model before it is sent to the client.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create a per-subscription <see cref="IServiceScope"/> for resolving interceptors — see remarks.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/> used for JSON serialization.</param>
/// <param name="healthTracker">The <see cref="IQueryHealthTracker"/> used to track subscription health.</param>
/// <param name="emissionGuards">The <see cref="IObservableQueryEmissionGuards"/> consulted per emission when an application opts in with an <see cref="IGuardObservableQueryEmission"/>.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class ObservableQueryDemultiplexer(
    IQueryPipeline queryPipeline,
    IQueryContextManager queryContextManager,
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IHostApplicationLifetime hostApplicationLifetime,
    IReadModelInterceptors readModelInterceptors,
    IServiceProvider serviceProvider,
    IOptions<ArcOptions> arcOptions,
    IQueryHealthTracker healthTracker,
    IObservableQueryEmissionGuards emissionGuards,
    ILogger<ObservableQueryDemultiplexer> logger) : IObservableQueryDemultiplexer
{
    const int WebSocketBufferSize = 1024 * 4;

    readonly ConcurrentDictionary<string, SSEConnectionState> _sseConnections = new();
    readonly ChangeSetComputor _changeSetComputor = new(arcOptions.Value.JsonSerializerOptions);
    int _nextConnectionId;

    /// <inheritdoc/>
    public async Task HandleWebSocketConnection(IHttpRequestContext context)
    {
        logger.WebSocketClientConnected();

        httpRequestContextAccessor.Current = context;

        var connectionId = $"ws-{Interlocked.Increment(ref _nextConnectionId)}";
        var webSocket = await context.WebSockets.AcceptWebSocket(context.RequestAborted);
        var subscriptions = new ConcurrentDictionary<string, IDisposable>();
        var writeLock = new SemaphoreSlim(1, 1);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            hostApplicationLifetime.ApplicationStopping);

        var keepAliveTracker = new KeepAliveTracker();
        var keepAliveTask = Task.CompletedTask;

        try
        {
#pragma warning disable CA2025 // keepAliveTask is always awaited in the finally block before linkedCts is disposed
            keepAliveTask = RunWebSocketKeepAlive(webSocket, keepAliveTracker, writeLock, linkedCts.Token);
#pragma warning restore CA2025
            await ReadWebSocketMessages(webSocket, subscriptions, context, connectionId, keepAliveTracker, writeLock, linkedCts.Token);
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

            healthTracker.RemoveConnection(connectionId);
            writeLock.Dispose();

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

        var keepAliveTask = RunSseKeepAlive(context, state.KeepAliveTracker, linkedCts, state.WriteLock);

        try
        {
            // Send the Connected message so the client knows its connection ID for POST requests, and the
            // keep-alive interval it should expect messages on.
            await SendSseMessage(
                context,
                ObservableQueryHubMessage.CreateConnected(connectionId, arcOptions.Value.Query.KeepAliveInterval),
                state.KeepAliveTracker,
                linkedCts,
                state.WriteLock);

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

            // Signal cancellation and drain the keep-alive loop before disposing the per-subscription
            // resources, so an in-flight emission observes the cancellation and stops touching the write lock
            // and cancellation source before they are torn down, rather than racing against their disposal.
            await linkedCts.CancelAsync();
            await keepAliveTask;

            foreach (var subscription in state.Subscriptions.Values)
            {
                subscription.Dispose();
            }

            state.Subscriptions.Clear();
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
            healthTracker.UnregisterSubscription(body.ConnectionId, body.QueryId);
        }

        var wasUnauthorized = false;

        var subscription = await CreateSubscription(
            state.Context,
            body.QueryId,
            body.Request,
            async result =>
            {
                var msg = ObservableQueryHubMessage.CreateQueryResult(body.QueryId, result);
                await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource, state.WriteLock);
                healthTracker.RecordDataServed(body.ConnectionId, body.QueryId);
            },
            async (id, errorMsg) =>
            {
                var msg = ObservableQueryHubMessage.CreateError(id, errorMsg);
                await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource, state.WriteLock);
            },
            async id =>
            {
                wasUnauthorized = true;
                var msg = ObservableQueryHubMessage.CreateUnauthorized(id);
                await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource, state.WriteLock);

                // Reached at subscribe time (nothing is tracked yet, so this is a no-op) and again when an emission
                // guard denies the running stream. Only this query's entry is torn down — every sibling subscription
                // on the same connection keeps streaming.
                TerminateSubscription(state.Subscriptions, body.ConnectionId, id);
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

            // Register subscription with health tracker
            var metadata = CreateSubscriptionMetadata(body.QueryId, body.Request, context, "SSE");
            healthTracker.RegisterSubscription(body.ConnectionId, "SSE", metadata);
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
            healthTracker.UnregisterSubscription(body.ConnectionId, body.QueryId);
        }

        context.SetStatusCode(200);
    }

    /// <summary>
    /// Subscribes to a streaming query result — an <see cref="ISubject{T}"/> or an <see cref="IAsyncEnumerable{T}"/>
    /// — and returns a disposable that tears the subscription down.
    /// </summary>
    /// <param name="context">The subscribing connection's <see cref="IHttpRequestContext"/>.</param>
    /// <param name="streamingData">The streaming result to subscribe to.</param>
    /// <param name="queryId">The id of the query the subscription is for.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> to report with each result.</param>
    /// <param name="transferMode">The transfer mode (for example delta or full), or null for the default.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> to stamp each result with.</param>
    /// <param name="identity">The <see cref="ObservableQuerySubscriptionIdentity"/> describing the query and the caller that established the subscription.</param>
    /// <param name="onNext">Callback invoked with each streamed <see cref="QueryResult"/>.</param>
    /// <param name="onError">Callback invoked with the query id and message when the subscription errors.</param>
    /// <param name="onUnauthorized">Callback invoked with the query id when an emission guard denies the stream, ending the subscription.</param>
    /// <param name="token">The connection's <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that stops the subscription, or null when the data is not streamable.</returns>
    internal IDisposable? SubscribeToStreamingData(
        IHttpRequestContext context,
        object streamingData,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        Func<string, Task> onUnauthorized,
        CancellationToken token)
    {
        var type = streamingData.GetType();

        if (type.ImplementsOpenGeneric(typeof(ISubject<>)))
        {
            return SubscribeToSubject(context, streamingData, type, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, token);
        }

        if (type.ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)))
        {
            // Drive the background stream from a per-subscription token linked to the connection's, and hand back a
            // disposable that cancels it. Returning null here left the stream untracked: unsubscribe could not stop
            // it, and re-subscribing the same query started a second one while the first kept pushing results.
            var streamCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            IDisposable subscription = new StreamingQuerySubscription(streamCancellationTokenSource);
            IServiceScope? guardScope = null;

            // A scope of this subscription's own for the emission guards to resolve from — created only when a guard
            // exists, so a stream without one keeps exactly the shape (and cost) it had before guards were a thing.
            // Ownership moves to the composite, which the connection disposes together with the subscription.
            if (emissionGuards.HasGuards)
            {
#pragma warning disable CA2000 // guardScope's ownership is transferred to the returned CompositeDisposable
                guardScope = serviceProvider.CreateScope();
#pragma warning restore CA2000
                subscription = new CompositeDisposable(subscription, guardScope);
            }

            _ = StreamAsyncEnumerable(streamingData, type, queryId, paging, correlationId, identity, guardScope?.ServiceProvider, onNext, onError, onUnauthorized, streamCancellationTokenSource.Token);

            return subscription;
        }

        return null;
    }

    async Task ReadWebSocketMessages(
        IWebSocket webSocket,
        ConcurrentDictionary<string, IDisposable> subscriptions,
        IHttpRequestContext context,
        string connectionId,
        KeepAliveTracker keepAliveTracker,
        SemaphoreSlim writeLock,
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

                await ProcessWebSocketMessage(message, webSocket, subscriptions, context, connectionId, keepAliveTracker, writeLock, token);
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
        string connectionId,
        KeepAliveTracker keepAliveTracker,
        SemaphoreSlim writeLock,
        CancellationToken token)
    {
        // Any inbound message from the client counts as activity — no keep-alive needed.
        keepAliveTracker.RecordActivity();

        switch (message.Type)
        {
            case ObservableQueryHubMessageType.Subscribe:
                await HandleWebSocketSubscribe(message, webSocket, subscriptions, context, connectionId, keepAliveTracker, writeLock, token);
                break;

            case ObservableQueryHubMessageType.Unsubscribe:
                HandleWebSocketUnsubscribe(message, subscriptions, connectionId);
                break;

            case ObservableQueryHubMessageType.Ping:
                await SendWebSocketMessage(webSocket, ObservableQueryHubMessage.CreatePong(message.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), keepAliveTracker, writeLock, token);
                break;
        }
    }

    async Task HandleWebSocketSubscribe(
        ObservableQueryHubMessage message,
        IWebSocket webSocket,
        ConcurrentDictionary<string, IDisposable> subscriptions,
        IHttpRequestContext context,
        string connectionId,
        KeepAliveTracker keepAliveTracker,
        SemaphoreSlim writeLock,
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
            healthTracker.UnregisterSubscription(connectionId, message.QueryId);
        }

        var queryId = message.QueryId ?? Guid.NewGuid().ToString();

        var subscription = await CreateSubscription(
            context,
            queryId,
            request,
            async result =>
            {
                var msg = ObservableQueryHubMessage.CreateQueryResult(queryId, result);
                await SendWebSocketMessage(webSocket, msg, keepAliveTracker, writeLock, token);
                healthTracker.RecordDataServed(connectionId, queryId);
            },
            async (id, errorMsg) =>
            {
                var msg = ObservableQueryHubMessage.CreateError(id, errorMsg);
                await SendWebSocketMessage(webSocket, msg, keepAliveTracker, writeLock, token);
            },
            async id =>
            {
                var msg = ObservableQueryHubMessage.CreateUnauthorized(id);
                await SendWebSocketMessage(webSocket, msg, keepAliveTracker, writeLock, token);

                // Reached at subscribe time (nothing is tracked yet, so this is a no-op) and again when an emission
                // guard denies the running stream. Only this query's entry is torn down — every sibling subscription
                // on the same connection keeps streaming.
                TerminateSubscription(subscriptions, connectionId, id);
            },
            token);

        if (subscription is not null)
        {
            subscriptions[queryId] = subscription;

            // Register subscription with health tracker
            var metadata = CreateSubscriptionMetadata(queryId, request, context, "WebSocket");
            healthTracker.RegisterSubscription(connectionId, "WebSocket", metadata);
        }
    }

    /// <summary>
    /// Tears down a single tracked subscription, leaving every other subscription on the connection untouched.
    /// </summary>
    /// <param name="subscriptions">The connection's tracked subscriptions.</param>
    /// <param name="connectionId">The id of the connection the subscription belongs to.</param>
    /// <param name="queryId">The id of the query whose subscription is torn down.</param>
    void TerminateSubscription(ConcurrentDictionary<string, IDisposable> subscriptions, string connectionId, string queryId)
    {
        if (subscriptions.TryRemove(queryId, out var subscription))
        {
            subscription.Dispose();
            healthTracker.UnregisterSubscription(connectionId, queryId);
        }
    }

    void HandleWebSocketUnsubscribe(
        ObservableQueryHubMessage message,
        ConcurrentDictionary<string, IDisposable> subscriptions,
        string connectionId)
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
            healthTracker.UnregisterSubscription(connectionId, queryId);
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
                CorrelationId = queryResult.CorrelationId,
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

        // The pipeline coerces the raw string arguments to their declared parameter types and publishes them on the
        // query context. Take them from there so an emission guard sees the same typed arguments the query itself ran
        // with, rather than the unconverted strings that came in over the wire.
        var performedQueryContext = queryContextManager.Current;
        var identity = new ObservableQuerySubscriptionIdentity(
            fullyQualifiedName,
            performedQueryContext?.Arguments ?? arguments,
            context.User);

        return SubscribeToStreamingData(context, streamingData, queryId, queryResult.Paging, request.TransferMode, queryResult.CorrelationId, identity, onNext, onError, onUnauthorized, token);
    }

    IDisposable SubscribeToSubject(
        IHttpRequestContext context,
        object subject,
        Type subjectType,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        Func<string, Task> onUnauthorized,
        CancellationToken token)
    {
        var elementType = subjectType.GetInterfaces()
            .First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(ISubject<>))
            .GetGenericArguments()[0];

        var method = typeof(ObservableQueryDemultiplexer)
            .GetMethod(nameof(SubscribeToSubjectOfType), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        return (IDisposable)method.Invoke(this, [context, subject, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, token])!;
    }

    CompositeDisposable SubscribeToSubjectOfType<T>(
        IHttpRequestContext context,
        ISubject<T> subject,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        Func<string, Task> onUnauthorized,
        CancellationToken token)
    {
        IEnumerable<object>? previousItems = null;
        var hasDeliveredEmission = false;
        var isTerminated = false;
        var isDeltaMode = string.Equals(transferMode, "delta", StringComparison.OrdinalIgnoreCase);
        var isFullMode = string.Equals(transferMode, "full", StringComparison.OrdinalIgnoreCase);

        // Interception (compliance/PII release) is asynchronous. Emissions are serialized through this gate so
        // the per-emission interception and the ChangeSet's previous/current item bookkeeping stay ordered even
        // when the underlying subject delivers the next value before the previous one has finished interception.
        var emissionGate = new SemaphoreSlim(1, 1);

        // Capture the per-subscription query context here, while the AsyncLocal still carries the
        // context set up by the query pipeline. Observer callbacks below are invoked from the MongoDB
        // change-stream thread, where AsyncLocal flow does not reach, so reading the manager from
        // inside the callback would return QueryContext.NotSet and overwrite the real paging info.
        var subscriptionQueryContext = queryContextManager.Current;

        // A scope of its own per subscription — see the class remarks — so this subscription's Chronicle
        // namespace resolution is never shared with (or overwritten by) another subscription's tenant.
        var interceptionScope = serviceProvider.CreateScope();

        var subscription = subject.Subscribe(OnEmission, OnSubscriptionError);

        return new CompositeDisposable(subscription, emissionGate, interceptionScope);

        // This is an async void callback invoked by the subject on a background (ThreadPool) thread. Any
        // exception that escapes it is unobserved and terminates the whole process. The connection's
        // per-subscription resources (the emission gate, the write lock and the linked cancellation source)
        // are disposed the moment the client disconnects, and an emission already in flight then races against
        // that disposal — touching a disposed SemaphoreSlim or CancellationTokenSource throws
        // ObjectDisposedException. The entire body is therefore wrapped so that nothing can propagate: the
        // expected disconnect signals (cancellation, disposal, broken transport) become no-ops, and only a
        // genuine failure is surfaced to the subscriber.
        async void OnEmission(T data)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var gateAcquired = false;
            try
            {
                await emissionGate.WaitAsync(token);
                gateAcquired = true;

                // An emission that was already queued behind the gate when a guard terminated the subscription must
                // not still be written — the client has been told it is no longer authorized.
                if (isTerminated)
                {
                    return;
                }

                // Restore the subscribing connection's context — this callback runs on the subject's own
                // producer thread, where the AsyncLocal tenant context set up when the subscription was
                // created does not flow (see the class remarks).
                httpRequestContextAccessor.Current = context;
                var interceptedData = await readModelInterceptors.InterceptEmission(typeof(T), data, interceptionScope.ServiceProvider);

                // Ask the emission guards before the delta baseline below moves. Advancing the baseline first would
                // fold a withheld emission's changes into "already delivered", so a later allowed emission would
                // compute its ChangeSet against state the client never received and silently lose those changes.
                if (emissionGuards.HasGuards)
                {
                    var verdict = await emissionGuards.Guard(new ObservableQueryEmissionContext(
                        identity.QueryName,
                        identity.Arguments,
                        identity.Principal,
                        correlationId,
                        interceptionScope.ServiceProvider,
                        !hasDeliveredEmission,
                        token));

                    if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
                    {
                        isTerminated = true;
                        logger.EmissionDenied(queryId);
                        await onUnauthorized(queryId);
                        return;
                    }

                    if (verdict == ObservableQueryEmissionVerdict.Suppress)
                    {
                        logger.EmissionSuppressed(queryId);
                        return;
                    }
                }

                var isFirstEmission = previousItems is null;

                // Compute ChangeSet for enumerable results (excludes single-item and string results).
                // Delta mode: skip computation on first emission (full snapshot is sent instead).
                // Full mode: skip computation entirely (client always receives the full snapshot).
                ChangeSet? changeSet = null;
                if (interceptedData is IEnumerable enumerable and not string)
                {
                    var currentItems = enumerable.Cast<object>().ToArray();
                    if (!isFullMode && (!isDeltaMode || !isFirstEmission))
                    {
                        changeSet = _changeSetComputor.Compute(previousItems, currentItems);
                    }

                    previousItems = currentItems;
                }

                var result = new QueryResult
                {
                    CorrelationId = correlationId,

                    // Delta mode subsequent emissions omit Data; client reconstructs from ChangeSet.
                    // First emission always includes the full snapshot regardless of mode.
                    Data = isDeltaMode && !isFirstEmission ? null! : interceptedData!,
                    IsAuthorized = true,
                    ValidationResults = [],
                    ExceptionMessages = [],
                    ExceptionStackTrace = string.Empty,
                    Paging = paging,

                    // Delta mode first emission: no ChangeSet (full snapshot is the initial state).
                    // Full mode: no ChangeSet (Data is always the complete current state).
                    // Legacy/delta subsequent: include computed ChangeSet.
                    ChangeSet = isFullMode || (isDeltaMode && isFirstEmission) ? null : changeSet
                };

                if (subscriptionQueryContext is not null && subscriptionQueryContext != QueryContext.NotSet)
                {
                    result.Paging = new PagingInfo(
                        subscriptionQueryContext.Paging.Page,
                        subscriptionQueryContext.Paging.Size,
                        subscriptionQueryContext.TotalItems);
                }

                await onNext(result);
                hasDeliveredEmission = true;
            }
            catch (OperationCanceledException)
            {
                // The connection was cancelled — expected when the client disconnects. No-op.
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (ObjectDisposedException)
            {
                // A per-subscription resource (emission gate / write lock / cancellation source) was disposed
                // while this emission was in flight — expected when the client disconnects mid-emission. This
                // must never escape an async void callback, so it is swallowed and treated as a no-op.
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (IOException)
            {
                // Transport error (broken pipe, reset connection) — the client disconnected. Ignored.
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (Exception error)
            {
                // Anything else is a genuine failure that must be surfaced to the subscriber.
                if (!token.IsCancellationRequested)
                {
                    logger.SubscriptionError(queryId, error);
                    _ = onError(queryId, error.Message);
                }
            }
            finally
            {
                if (gateAcquired)
                {
                    try
                    {
                        emissionGate.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // The emission gate was disposed while this emission was in flight — expected on
                        // client disconnect. There is nothing to release.
                    }
                }
            }
        }

        void OnSubscriptionError(Exception error)
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
        }
    }

    async Task StreamAsyncEnumerable(
        object enumerable,
        Type enumerableType,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        Func<string, Task> onUnauthorized,
        CancellationToken token)
    {
        var elementType = enumerableType.GetInterfaces()
            .First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            .GetGenericArguments()[0];

        var method = typeof(ObservableQueryDemultiplexer)
            .GetMethod(nameof(StreamAsyncEnumerableOfType), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        await (Task)method.Invoke(this, [enumerable, queryId, paging, correlationId, identity, guardServiceProvider, onNext, onError, onUnauthorized, token])!;
    }

    async Task StreamAsyncEnumerableOfType<T>(
        IAsyncEnumerable<T> enumerable,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, Task> onNext,
        Func<string, string, Task> onError,
        Func<string, Task> onUnauthorized,
        CancellationToken token)
    {
        var hasDeliveredEmission = false;

        try
        {
            await foreach (var item in enumerable.WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (guardServiceProvider is not null)
                {
                    var verdict = await emissionGuards.Guard(new ObservableQueryEmissionContext(
                        identity.QueryName,
                        identity.Arguments,
                        identity.Principal,
                        correlationId,
                        guardServiceProvider,
                        !hasDeliveredEmission,
                        token));

                    if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
                    {
                        logger.EmissionDenied(queryId);
                        await onUnauthorized(queryId);
                        return;
                    }

                    if (verdict == ObservableQueryEmissionVerdict.Suppress)
                    {
                        logger.EmissionSuppressed(queryId);
                        continue;
                    }
                }

                var result = new QueryResult
                {
                    CorrelationId = correlationId,
                    Data = item!,
                    IsAuthorized = true,
                    ValidationResults = [],
                    ExceptionMessages = [],
                    ExceptionStackTrace = string.Empty,
                    Paging = paging
                };

                await onNext(result);
                hasDeliveredEmission = true;
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
        catch (ObjectDisposedException)
        {
            // The connection's resources were disposed while streaming — expected on client disconnect.
        }
        catch (Exception ex)
        {
            logger.SubscriptionError(queryId, ex);
            await onError(queryId, ex.Message);
        }
    }

    async Task SendWebSocketMessage(IWebSocket webSocket, ObservableQueryHubMessage message, KeepAliveTracker keepAliveTracker, SemaphoreSlim writeLock, CancellationToken token)
    {
        var lockHeld = false;

        try
        {
            if (webSocket.State != WebSocketState.Open)
            {
                return;
            }

            await writeLock.WaitAsync(token);
            lockHeld = true;

#pragma warning disable CA1508 // Avoid dead conditional code
            if (webSocket.State != WebSocketState.Open)
            {
                return;
            }
#pragma warning restore CA1508 // Avoid dead conditional code

            var json = JsonSerializer.SerializeToUtf8Bytes(message, arcOptions.Value.JsonSerializerOptions);
            await webSocket.Send(new ArraySegment<byte>(json), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            keepAliveTracker.RecordMessageSent();
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown or token cancelled
        }
        catch (ObjectDisposedException)
        {
            // The write lock or the linked cancellation source was disposed as the connection ended —
            // expected when the client disconnects. Nothing to send.
        }
        catch (Exception ex)
        {
            logger.ErrorSendingMessage(ex);
        }
        finally
        {
            if (lockHeld)
            {
                try
                {
                    writeLock.Release();
                }
                catch (ObjectDisposedException)
                {
                    // The write lock was disposed while sending — expected on client disconnect.
                }
            }
        }
    }

    async Task SendSseMessage(
        IHttpRequestContext context,
        ObservableQueryHubMessage message,
        KeepAliveTracker keepAliveTracker,
        CancellationTokenSource cts,
        SemaphoreSlim writeLock)
    {
        var writeLockHeld = false;

        try
        {
            await writeLock.WaitAsync(cts.Token);
            writeLockHeld = true;

            var json = JsonSerializer.Serialize(message, arcOptions.Value.JsonSerializerOptions);
            await context.Write($"data: {json}\n\n", cts.Token);
            keepAliveTracker.RecordMessageSent();
        }
        catch (HttpListenerException)
        {
            // Client disconnected — cancel the connection token source to trigger cleanup.
            await CancelQuietly(cts);
        }
        catch (IOException)
        {
            // On macOS and some .NET runtimes, HttpListener throws IOException for broken-pipe
            // instead of HttpListenerException — treat identically.
            await CancelQuietly(cts);
        }
        catch (ArgumentNullException ex) when (ex.ParamName == "array")
        {
            // StreamPipeWriter can throw this during response teardown when writes race with transport shutdown.
            await CancelQuietly(cts);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
        catch (ObjectDisposedException)
        {
            // The linked cancellation source (or write lock) was disposed as the connection ended — expected
            // when the client disconnects mid-write. Nothing to send.
        }
        catch (Exception ex)
        {
            logger.ErrorSendingMessage(ex);
        }
        finally
        {
            if (writeLockHeld)
            {
                try
                {
                    writeLock.Release();
                }
                catch (ObjectDisposedException)
                {
                    // The write lock was disposed while sending — expected on client disconnect.
                }
            }
        }
    }

    Task RunWebSocketKeepAlive(IWebSocket webSocket, KeepAliveTracker keepAliveTracker, SemaphoreSlim writeLock, CancellationToken token) =>
        RunKeepAlive(
            keepAliveTracker,
            () => SendWebSocketMessage(webSocket, ObservableQueryHubMessage.CreatePing(), keepAliveTracker, writeLock, token),
            token);

    Task RunSseKeepAlive(IHttpRequestContext context, KeepAliveTracker keepAliveTracker, CancellationTokenSource cts, SemaphoreSlim writeLock) =>
        RunKeepAlive(
            keepAliveTracker,
            () => SendSseMessage(context, ObservableQueryHubMessage.CreatePing(), keepAliveTracker, cts, writeLock),
            cts.Token);

    /// <summary>
    /// Runs the transport-agnostic keep-alive loop, guaranteeing that no more than the configured
    /// interval passes between messages sent to the client.
    /// </summary>
    /// <param name="keepAliveTracker">The <see cref="KeepAliveTracker"/> recording when messages were last sent.</param>
    /// <param name="sendKeepAlive">Sends a keep-alive message over the transport.</param>
    /// <param name="token">The <see cref="CancellationToken"/> that ends the loop.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The loop waits until a keep-alive is actually due relative to the last message sent, rather than
    /// waking on a fixed interval grid. A fixed grid defers the keep-alive to the next tick whenever a
    /// data message lands mid-interval, which lets the gap between messages grow to nearly twice the
    /// interval and causes clients watching for silence to drop an otherwise healthy connection.
    /// </remarks>
    async Task RunKeepAlive(KeepAliveTracker keepAliveTracker, Func<Task> sendKeepAlive, CancellationToken token)
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
                var remaining = keepAliveTracker.GetTimeUntilNextKeepAlive(interval);

                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, token);
                    continue;
                }

                await sendKeepAlive();

                // A successful send records activity, so the next iteration waits a full interval.
                // If the send failed without recording anything, wait one interval anyway so that a
                // persistently failing transport cannot spin this loop.
                if (keepAliveTracker.GetTimeUntilNextKeepAlive(interval) <= TimeSpan.Zero)
                {
                    await Task.Delay(interval, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
    }

#pragma warning disable SA1204 // Static members should appear before non-static members

    /// <summary>
    /// Cancels a <see cref="CancellationTokenSource"/> while tolerating it already having been disposed as the
    /// connection ended — cancelling a disposed source would otherwise throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    /// <param name="cts">The <see cref="CancellationTokenSource"/> to cancel.</param>
    /// <returns>Awaitable task.</returns>
    static async Task CancelQuietly(CancellationTokenSource cts)
    {
        try
        {
            await cts.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed as the connection ended — nothing to cancel.
        }
    }

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

    QuerySubscriptionMetadata CreateSubscriptionMetadata(
        string subscriptionId,
        ObservableQuerySubscriptionRequest request,
        IHttpRequestContext context,
        string protocol)
    {
        var lastDotIndex = request.QueryName.LastIndexOf('.');
        var readModelType = lastDotIndex >= 0 ? request.QueryName[..lastDotIndex] : request.QueryName;

        return new QuerySubscriptionMetadata
        {
            SubscriptionId = subscriptionId,
            QueryIdentifier = request.QueryName,
            ReadModelType = readModelType,
            ConnectedAt = DateTimeOffset.UtcNow,
            ClientInfo = new QuerySubscriptionClientInfo
            {
                RemoteIpAddress = context.Headers.GetValueOrDefault("X-Forwarded-For") ??
                                  context.Headers.GetValueOrDefault("X-Real-IP") ??
                                  "unknown",
                UserAgent = context.Headers.GetValueOrDefault("User-Agent"),
                UserId = context.User?.Identity?.Name,
                Protocol = protocol
            }
        };
    }
#pragma warning restore SA1204

    sealed record SSEConnectionState(
        IHttpRequestContext Context,
        CancellationTokenSource CancellationTokenSource)
    {
        public ConcurrentDictionary<string, IDisposable> Subscriptions { get; } = new();
        public KeepAliveTracker KeepAliveTracker { get; } = new();
        public SemaphoreSlim WriteLock { get; } = new(1, 1);
    }
}
