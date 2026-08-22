// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Security.Claims;
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
        var subscriptions = new ObservableQuerySubscriptionStates();
        var writeLock = new SemaphoreSlim(1, 1);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            hostApplicationLifetime.ApplicationStopping);

        var keepAliveTracker = new KeepAliveTracker();
        var keepAliveTask = Task.CompletedTask;

        try
        {
            // Advertise capabilities before reading the client's initial legacy subscriptions. A capable client can
            // then replace them with revision-aware subscriptions without losing compatibility with older servers.
            await SendWebSocketMessage(
                webSocket,
                ObservableQueryHubMessage.CreateConnected(connectionId, arcOptions.Value.Query.KeepAliveInterval),
                keepAliveTracker,
                writeLock,
                linkedCts.Token);

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
            subscriptions.Dispose();

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

#pragma warning disable CA2025 // keepAliveTask is always awaited in the finally block before linkedCts is disposed
        var keepAliveTask = RunSseKeepAlive(context, state.KeepAliveTracker, linkedCts, state.WriteLock);
#pragma warning restore CA2025

        try
        {
            // Send the Connected message so the client knows its connection ID for POST requests, and the
            // keep-alive interval it should expect messages on.
            await SendSseMessage(
                context,
                ObservableQueryHubMessage.CreateConnected(connectionId, arcOptions.Value.Query.KeepAliveInterval),
                state.KeepAliveTracker,
                linkedCts,
                state.WriteLock,
                linkedCts.Token);

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

            state.Subscriptions.Dispose();
        }

        logger.SseClientDisconnected(connectionId);
    }

    /// <inheritdoc/>
    public async Task HandleSSESubscribe(IHttpRequestContext context)
    {
        ObservableQuerySSESubscribeRequest? body;
        try
        {
            body = await context.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), context.RequestAborted)
                as ObservableQuerySSESubscribeRequest;
        }
        catch (Exception ex) when (
            context.RequestAborted.IsCancellationRequested &&
            ex is OperationCanceledException or IOException)
        {
            return;
        }

        if (body is null
            || string.IsNullOrEmpty(body.ConnectionId)
            || string.IsNullOrEmpty(body.QueryId)
            || body.Request is null
            || !ObservableQuerySubscriptionRevision.IsValid(body.Revision))
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

        // Reserve this query id before awaiting the query pipeline. Revision ordering makes duplicates idempotent,
        // ignores stale work, and lets an unsubscribe tombstone arrive before its delayed subscribe.
        var operation = ReserveSubscription(
            state.Subscriptions,
            body.QueryId,
            body.Revision,
            state.CancellationToken);
        if (operation is null)
        {
            context.SetStatusCode(200);
            return;
        }

        logger.ClientSubscribed(body.Request.QueryName, body.QueryId);
        var subscriptionContext = new ObservableQuerySubscriptionHttpRequestContext(
            context,
            state.Context,
            serviceProvider,
            operation.Token);
        var previousContext = httpRequestContextAccessor.Current;
        var wasUnauthorized = false;

        try
        {
            httpRequestContextAccessor.Current = subscriptionContext;

            var subscription = await CreateSubscription(
                subscriptionContext,
                context.RequestServices,
                subscriptionContext.GetPrincipal(),
                body.QueryId,
                body.Request,
                async (result, operationToken) =>
                {
                    if (!IsCurrent(state.Subscriptions, body.QueryId, operation))
                    {
                        return;
                    }

                    var msg = ObservableQueryHubMessage.CreateQueryResult(body.QueryId, result, operation.Revision);
                    if (await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource, state.WriteLock, operationToken) &&
                        IsCurrent(state.Subscriptions, body.QueryId, operation))
                    {
                        healthTracker.RecordDataServed(body.ConnectionId, body.QueryId);
                    }
                },
                async (id, errorMsg, operationToken) =>
                {
                    if (!IsCurrent(state.Subscriptions, id, operation))
                    {
                        return;
                    }

                    var msg = ObservableQueryHubMessage.CreateError(id, errorMsg, operation.Revision);
                    await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource, state.WriteLock, operationToken);
                },
                async (id, operationToken) =>
                {
                    if (!IsCurrent(state.Subscriptions, id, operation))
                    {
                        return;
                    }

                    operationToken.ThrowIfCancellationRequested();
                    wasUnauthorized = true;
                    var msg = ObservableQueryHubMessage.CreateUnauthorized(id, operation.Revision);
                    await SendSseMessage(state.Context, msg, state.KeepAliveTracker, state.CancellationTokenSource, state.WriteLock, operationToken);

                    TerminateSubscription(state.Subscriptions, id, operation);
                },
                operation.Token);

            if (subscription is not null && operation.TryAttach(subscription) && IsCurrent(state.Subscriptions, body.QueryId, operation))
            {
                var metadata = CreateSubscriptionMetadata(body.QueryId, body.Request, subscriptionContext, "SSE");
                operation.TryRegister(
                    () => healthTracker.RegisterSubscription(body.ConnectionId, "SSE", metadata),
                    () => healthTracker.UnregisterSubscription(body.ConnectionId, body.QueryId));
            }
            else if (subscription is null)
            {
                TerminateSubscription(state.Subscriptions, body.QueryId, operation);
            }
        }
        catch (Exception ex) when (
            operation.Token.IsCancellationRequested &&
            ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
            // The operation was replaced or the connection ended while creation was in flight.
        }
        finally
        {
            httpRequestContextAccessor.Current = previousContext;
            if (operation.Token.IsCancellationRequested)
            {
                TerminateSubscription(state.Subscriptions, body.QueryId, operation);
            }
            operation.CompleteCreation();
        }

        context.SetStatusCode(wasUnauthorized ? 401 : 200);
    }

    /// <inheritdoc/>
    public async Task HandleSSEUnsubscribe(IHttpRequestContext context)
    {
        if (await context.ReadBodyAsJson(typeof(ObservableQuerySSEUnsubscribeRequest), context.RequestAborted)
            is not ObservableQuerySSEUnsubscribeRequest body
            || string.IsNullOrEmpty(body.ConnectionId)
            || string.IsNullOrEmpty(body.QueryId)
            || !ObservableQuerySubscriptionRevision.IsValid(body.Revision))
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

        var accepted = state.Subscriptions.TryUnsubscribe(body.QueryId, body.Revision);
        if (accepted)
        {
            logger.ClientUnsubscribed(body.QueryId);
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
    /// <param name="onNext">Callback invoked with each streamed <see cref="QueryResult"/> and the active subscription token.</param>
    /// <param name="onError">Callback invoked with the query id, message and active subscription token when the subscription errors.</param>
    /// <param name="onUnauthorized">Callback invoked with the query id and active subscription token when an emission guard denies the stream, ending the subscription.</param>
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
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        var type = streamingData.GetType();

        if (type.ImplementsOpenGeneric(typeof(ISubject<>)))
        {
            return SubscribeToSubject(context, streamingData, type, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, token);
        }

        if (type.ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)))
        {
            var lifetime = new StreamingQuerySubscription(token);

            IServiceScope? guardScope = null;
            if (emissionGuards.HasGuards)
            {
                var ownedScope = (IServiceScope?)serviceProvider.CreateScope();
                try
                {
                    lifetime.AddResource(ownedScope);
                    guardScope = ownedScope;
                    ownedScope = null;
                }
                finally
                {
                    ownedScope?.Dispose();
                }
            }

            if (lifetime.TryEnter())
            {
                _ = RunStream();
            }

            return lifetime;

            async Task RunStream()
            {
                try
                {
                    await StreamAsyncEnumerable(
                        context,
                        streamingData,
                        type,
                        queryId,
                        paging,
                        correlationId,
                        identity,
                        guardScope?.ServiceProvider,
                        onNext,
                        onError,
                        onUnauthorized,
                        lifetime.Token);
                }
                finally
                {
                    lifetime.Exit();
                }
            }
        }

        return null;
    }

    async Task ReadWebSocketMessages(
        IWebSocket webSocket,
        ObservableQuerySubscriptionStates subscriptions,
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
    }

    async Task ProcessWebSocketMessage(
        ObservableQueryHubMessage message,
        IWebSocket webSocket,
        ObservableQuerySubscriptionStates subscriptions,
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
                HandleWebSocketUnsubscribe(message, subscriptions);
                break;

            case ObservableQueryHubMessageType.Ping:
                await SendWebSocketMessage(webSocket, ObservableQueryHubMessage.CreatePong(message.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), keepAliveTracker, writeLock, token);
                break;
        }
    }

    async Task HandleWebSocketSubscribe(
        ObservableQueryHubMessage message,
        IWebSocket webSocket,
        ObservableQuerySubscriptionStates subscriptions,
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

        if (!ObservableQuerySubscriptionRevision.IsValid(message.Revision))
        {
            return;
        }

        var queryId = message.QueryId ?? Guid.NewGuid().ToString();
        var operation = ReserveSubscription(subscriptions, queryId, message.Revision, token);
        if (operation is null)
        {
            return;
        }

        logger.ClientSubscribed(request.QueryName, queryId);
        var subscriptionContext = new ObservableQuerySubscriptionHttpRequestContext(context, context, serviceProvider, operation.Token);
        var previousContext = httpRequestContextAccessor.Current;

        try
        {
            httpRequestContextAccessor.Current = subscriptionContext;
            var subscription = await CreateSubscription(
                subscriptionContext,
                context.RequestServices,
                subscriptionContext.GetPrincipal(),
                queryId,
                request,
                async (result, operationToken) =>
                {
                    if (!IsCurrent(subscriptions, queryId, operation))
                    {
                        return;
                    }

                    var msg = ObservableQueryHubMessage.CreateQueryResult(queryId, result, operation.Revision);
                    if (await SendWebSocketMessage(webSocket, msg, keepAliveTracker, writeLock, operationToken) &&
                        IsCurrent(subscriptions, queryId, operation))
                    {
                        healthTracker.RecordDataServed(connectionId, queryId);
                    }
                },
                async (id, errorMsg, operationToken) =>
                {
                    if (!IsCurrent(subscriptions, id, operation))
                    {
                        return;
                    }

                    var msg = ObservableQueryHubMessage.CreateError(id, errorMsg, operation.Revision);
                    await SendWebSocketMessage(webSocket, msg, keepAliveTracker, writeLock, operationToken);
                },
                async (id, operationToken) =>
                {
                    if (!IsCurrent(subscriptions, id, operation))
                    {
                        return;
                    }

                    operationToken.ThrowIfCancellationRequested();
                    var msg = ObservableQueryHubMessage.CreateUnauthorized(id, operation.Revision);
                    await SendWebSocketMessage(webSocket, msg, keepAliveTracker, writeLock, operationToken);
                    TerminateSubscription(subscriptions, id, operation);
                },
                operation.Token);

            if (subscription is not null && operation.TryAttach(subscription) && IsCurrent(subscriptions, queryId, operation))
            {
                var metadata = CreateSubscriptionMetadata(queryId, request, subscriptionContext, "WebSocket");
                operation.TryRegister(
                    () => healthTracker.RegisterSubscription(connectionId, "WebSocket", metadata),
                    () => healthTracker.UnregisterSubscription(connectionId, queryId));
            }
            else if (subscription is null)
            {
                TerminateSubscription(subscriptions, queryId, operation);
            }
        }
        catch (Exception ex) when (
            operation.Token.IsCancellationRequested &&
            ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
            // The operation was replaced or the connection ended while creation was in flight.
        }
        finally
        {
            httpRequestContextAccessor.Current = previousContext;
            if (operation.Token.IsCancellationRequested)
            {
                TerminateSubscription(subscriptions, queryId, operation);
            }
            operation.CompleteCreation();
        }
    }

    ObservableQuerySubscriptionOperation? ReserveSubscription(
        ObservableQuerySubscriptionStates subscriptions,
        string queryId,
        long? revision,
        CancellationToken connectionToken) =>
        subscriptions.TrySubscribe(queryId, revision, connectionToken);

    bool IsCurrent(
        ObservableQuerySubscriptionStates subscriptions,
        string queryId,
        ObservableQuerySubscriptionOperation operation) =>
        subscriptions.IsCurrent(queryId, operation);

    void TerminateSubscription(
        ObservableQuerySubscriptionStates subscriptions,
        string queryId,
        ObservableQuerySubscriptionOperation operation) =>
        subscriptions.Terminate(queryId, operation);

    void HandleWebSocketUnsubscribe(
        ObservableQueryHubMessage message,
        ObservableQuerySubscriptionStates subscriptions)
    {
        var queryId = message.QueryId;
        if (queryId is null)
        {
            return;
        }

        if (!ObservableQuerySubscriptionRevision.IsValid(message.Revision))
        {
            return;
        }

        var accepted = subscriptions.TryUnsubscribe(queryId, message.Revision);
        if (accepted)
        {
            logger.ClientUnsubscribed(queryId);
        }
    }

    async Task<IDisposable?> CreateSubscription(
        IHttpRequestContext context,
        IServiceProvider queryServiceProvider,
        ClaimsPrincipal principal,
        string queryId,
        ObservableQuerySubscriptionRequest request,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
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
            queryServiceProvider,
            token);

        if (!queryResult.IsAuthorized)
        {
            logger.QueryUnauthorized(request.QueryName, queryId);
            await onUnauthorized(queryId, token);
            return null;
        }

        if (!queryResult.IsSuccess)
        {
            var errorMsg = string.Join("; ", queryResult.ExceptionMessages);
            await onError(queryId, errorMsg, token);
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

            await onNext(queryResultWithData, token);
            return null;
        }

        // The pipeline coerces the raw string arguments to their declared parameter types and publishes them on the
        // query context. Take them from there so an emission guard sees the same typed arguments the query itself ran
        // with, rather than the unconverted strings that came in over the wire.
        var performedQueryContext = queryContextManager.Current;
        var identity = new ObservableQuerySubscriptionIdentity(
            fullyQualifiedName,
            performedQueryContext?.Arguments ?? arguments,
            principal);

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
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
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

    StreamingQuerySubscription SubscribeToSubjectOfType<T>(
        IHttpRequestContext context,
        ISubject<T> subject,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
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

        var interceptionScope = serviceProvider.CreateScope();
        var lifetime = new StreamingQuerySubscription(token);

        var ownedGate = (SemaphoreSlim?)emissionGate;
        var ownedScope = (IServiceScope?)interceptionScope;
        try
        {
            lifetime.AddResource(ownedGate);
            ownedGate = null;
            lifetime.AddResource(ownedScope);
            ownedScope = null;
        }
        finally
        {
            ownedGate?.Dispose();
            ownedScope?.Dispose();
        }

        var subscriptionToken = lifetime.Token;
        try
        {
            // BehaviorSubject can replay synchronously from inside Subscribe. The lifetime therefore exists before
            // observer attachment, and SetProducer disposes an observer whose replay terminated the operation.
            lifetime.SetProducer(subject.Subscribe(OnEmission, OnSubscriptionError));
        }
        catch
        {
            lifetime.Dispose();
            throw;
        }

        return lifetime;

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
            if (!lifetime.TryEnter())
            {
                return;
            }

            var gateAcquired = false;
            var previousContext = httpRequestContextAccessor.Current;
            try
            {
                await emissionGate.WaitAsync(subscriptionToken);
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
                subscriptionToken.ThrowIfCancellationRequested();

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
                        subscriptionToken));

                    subscriptionToken.ThrowIfCancellationRequested();

                    if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
                    {
                        isTerminated = true;
                        logger.EmissionDenied(queryId);
                        await onUnauthorized(queryId, subscriptionToken);
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

                subscriptionToken.ThrowIfCancellationRequested();
                await onNext(result, subscriptionToken);
                hasDeliveredEmission = true;
            }
            catch (Exception error) when (
                subscriptionToken.IsCancellationRequested &&
                error is OperationCanceledException or IOException or ObjectDisposedException)
            {
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (Exception error) when (!IsFatal(error))
            {
                logger.SubscriptionError(queryId, error);
                try
                {
                    await onError(queryId, error.Message, subscriptionToken);
                }
                catch (Exception callbackError) when (
                    subscriptionToken.IsCancellationRequested &&
                    callbackError is OperationCanceledException or IOException or ObjectDisposedException)
                {
                    logger.EmissionAfterDisconnect(queryId);
                }
                catch (Exception callbackError) when (!IsFatal(callbackError))
                {
                    logger.SubscriptionError(queryId, callbackError);
                }
            }
            finally
            {
                httpRequestContextAccessor.Current = previousContext;
                if (gateAcquired)
                {
                    emissionGate.Release();
                }
                lifetime.Exit();
            }
        }

        async void OnSubscriptionError(Exception error)
        {
            if (!lifetime.TryEnter())
            {
                return;
            }

            var previousContext = httpRequestContextAccessor.Current;
            try
            {
                httpRequestContextAccessor.Current = context;
                if (subscriptionToken.IsCancellationRequested)
                {
                    return;
                }

                logger.SubscriptionError(queryId, error);
                await onError(queryId, error.Message, subscriptionToken);
            }
            catch (Exception callbackError) when (
                subscriptionToken.IsCancellationRequested &&
                callbackError is OperationCanceledException or IOException or ObjectDisposedException)
            {
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (Exception callbackError) when (!IsFatal(callbackError))
            {
                logger.SubscriptionError(queryId, callbackError);
            }
            finally
            {
                httpRequestContextAccessor.Current = previousContext;
                lifetime.Exit();
            }
        }
    }

    async Task StreamAsyncEnumerable(
        IHttpRequestContext context,
        object enumerable,
        Type enumerableType,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        var elementType = enumerableType.GetInterfaces()
            .First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            .GetGenericArguments()[0];

        var method = typeof(ObservableQueryDemultiplexer)
            .GetMethod(nameof(StreamAsyncEnumerableOfType), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        await (Task)method.Invoke(this, [context, enumerable, queryId, paging, correlationId, identity, guardServiceProvider, onNext, onError, onUnauthorized, token])!;
    }

    async Task StreamAsyncEnumerableOfType<T>(
        IHttpRequestContext context,
        IAsyncEnumerable<T> enumerable,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        var hasDeliveredEmission = false;
        var previousContext = httpRequestContextAccessor.Current;

        try
        {
            httpRequestContextAccessor.Current = context;
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

                    token.ThrowIfCancellationRequested();

                    if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
                    {
                        logger.EmissionDenied(queryId);
                        await onUnauthorized(queryId, token);
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

                token.ThrowIfCancellationRequested();
                await onNext(result, token);
                token.ThrowIfCancellationRequested();
                hasDeliveredEmission = true;
            }
        }
        catch (Exception ex) when (
            token.IsCancellationRequested &&
            ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
            // The subscription ended while the stream or one of its callbacks was active.
        }
        catch (Exception ex)
        {
            logger.SubscriptionError(queryId, ex);
            try
            {
                await onError(queryId, ex.Message, token);
            }
            catch (Exception callbackError) when (
                token.IsCancellationRequested &&
                callbackError is OperationCanceledException or IOException or ObjectDisposedException)
            {
                // The subscription ended while reporting the live failure.
            }
            catch (Exception callbackError) when (!IsFatal(callbackError))
            {
                logger.SubscriptionError(queryId, callbackError);
            }
        }
        finally
        {
            httpRequestContextAccessor.Current = previousContext;
        }
    }

    async Task<bool> SendWebSocketMessage(IWebSocket webSocket, ObservableQueryHubMessage message, KeepAliveTracker keepAliveTracker, SemaphoreSlim writeLock, CancellationToken token)
    {
        var lockHeld = false;

        try
        {
            if (webSocket.State != WebSocketState.Open)
            {
                return false;
            }

            await writeLock.WaitAsync(token);
            lockHeld = true;

#pragma warning disable CA1508 // Avoid dead conditional code
            if (webSocket.State != WebSocketState.Open)
            {
                return false;
            }
#pragma warning restore CA1508 // Avoid dead conditional code

            var json = JsonSerializer.SerializeToUtf8Bytes(message, arcOptions.Value.JsonSerializerOptions);
            await webSocket.Send(new ArraySegment<byte>(json), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            token.ThrowIfCancellationRequested();
            keepAliveTracker.RecordMessageSent();
            return true;
        }
        catch (Exception ex) when (
            token.IsCancellationRequested &&
            ex is OperationCanceledException or ObjectDisposedException)
        {
            // Normal shutdown or subscription cancellation.
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
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    // The write lock was disposed after connection cancellation.
                }
                catch (ObjectDisposedException ex)
                {
                    logger.ErrorSendingMessage(ex);
                }
            }
        }

        return false;
    }

    async Task<bool> SendSseMessage(
        IHttpRequestContext context,
        ObservableQueryHubMessage message,
        KeepAliveTracker keepAliveTracker,
        CancellationTokenSource cts,
        SemaphoreSlim writeLock,
        CancellationToken operationToken)
    {
        var writeLockHeld = false;

        try
        {
            await writeLock.WaitAsync(operationToken);
            writeLockHeld = true;

            var json = JsonSerializer.Serialize(message, arcOptions.Value.JsonSerializerOptions);
            await context.Write($"data: {json}\n\n", operationToken);
            operationToken.ThrowIfCancellationRequested();
            keepAliveTracker.RecordMessageSent();
            return true;
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
        catch (Exception ex) when (
            operationToken.IsCancellationRequested &&
            ex is OperationCanceledException or ObjectDisposedException)
        {
            // Normal shutdown or subscription cancellation — nothing to report.
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
                catch (ObjectDisposedException) when (operationToken.IsCancellationRequested)
                {
                    // The write lock was disposed after connection cancellation.
                }
                catch (ObjectDisposedException ex)
                {
                    logger.ErrorSendingMessage(ex);
                }
            }
        }

        return false;
    }

    Task RunWebSocketKeepAlive(IWebSocket webSocket, KeepAliveTracker keepAliveTracker, SemaphoreSlim writeLock, CancellationToken token) =>
        RunKeepAlive(
            keepAliveTracker,
            () => SendWebSocketMessage(webSocket, ObservableQueryHubMessage.CreatePing(), keepAliveTracker, writeLock, token),
            token);

    Task RunSseKeepAlive(IHttpRequestContext context, KeepAliveTracker keepAliveTracker, CancellationTokenSource cts, SemaphoreSlim writeLock) =>
        RunKeepAlive(
            keepAliveTracker,
            () => SendSseMessage(context, ObservableQueryHubMessage.CreatePing(), keepAliveTracker, cts, writeLock, cts.Token),
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

    static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or
            StackOverflowException or
            AccessViolationException or
            AppDomainUnloadedException or
            BadImageFormatException or
            CannotUnloadAppDomainException or
            InvalidProgramException;

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
        ObservableQuerySubscriptionHttpRequestContext context,
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
        public ObservableQuerySubscriptionStates Subscriptions { get; } = new();
        public CancellationToken CancellationToken { get; } = CancellationTokenSource.Token;
        public KeepAliveTracker KeepAliveTracker { get; } = new();
        public SemaphoreSlim WriteLock { get; } = new(1, 1);
    }
}
