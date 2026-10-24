// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

public partial class ObservableQueryDemultiplexer
{
    /// <inheritdoc/>
    public async Task HandleWebSocketConnection(IHttpRequestContext context)
    {
        logger.WebSocketClientConnected();

        using var request = HttpRequestContextAccessor.BeginRequest(httpRequestContextAccessor, context);

        var connectionId = $"ws-{Interlocked.Increment(ref _nextConnectionId)}";
        var webSocket = await context.WebSockets.AcceptWebSocket(context.RequestAborted);
        var subscriptions = new ObservableQuerySubscriptionStates();
        var writeLock = new SemaphoreSlim(1, 1);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            hostApplicationLifetime.ApplicationStopping);

        var keepAliveTracker = new KeepAliveTracker();
        var connection = new WebSocketConnection(webSocket, subscriptions, context, connectionId, keepAliveTracker, writeLock);
        var keepAliveTask = Task.CompletedTask;

        try
        {
            // Advertise capabilities before reading the client's initial legacy subscriptions. A capable client can
            // then replace them with revision-aware subscriptions without losing compatibility with older servers.
            await SendWebSocketMessage(
                connection,
                ObservableQueryHubMessage.CreateConnected(connectionId, arcOptions.Value.Query.KeepAliveInterval),
                linkedCts.Token);

#pragma warning disable CA2025 // keepAliveTask is always awaited in the finally block before linkedCts is disposed
            keepAliveTask = RunWebSocketKeepAlive(connection, linkedCts.Token);
#pragma warning restore CA2025
            await ReadWebSocketMessages(connection, linkedCts.Token);
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
            await connection.WhenIdle();
            subscriptions.Dispose();

            healthTracker.RemoveConnection(connectionId);
            writeLock.Dispose();
            connection.Concurrency.Dispose();

            logger.WebSocketClientDisconnected();
        }
    }

    static async Task<WebSocketReceiveResult?> TryReceiveWebSocketMessage(IWebSocket webSocket, byte[] buffer, CancellationToken token)
    {
        try
        {
            return await webSocket.Receive(new ArraySegment<byte>(buffer), token);
        }
        catch (WebSocketException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    async Task ReadWebSocketMessages(WebSocketConnection connection, CancellationToken token)
    {
        var webSocket = connection.WebSocket;
        var buffer = new byte[WebSocketBufferSize];

        while (!token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var received = await TryReceiveWebSocketMessage(webSocket, buffer, token);
            if (received is null)
            {
                break;
            }

            if (received.CloseStatus.HasValue)
            {
                await webSocket.Close(received.CloseStatus.Value, received.CloseStatusDescription, token);
                break;
            }

            if (received.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
            {
                await TryProcessWebSocketText(connection, buffer, received.Count, token);
            }
        }
    }

    async Task TryProcessWebSocketText(WebSocketConnection connection, byte[] buffer, int count, CancellationToken token)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(buffer, 0, count);
            var message = JsonSerializer.Deserialize(json, arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<ObservableQueryHubMessage>());

            if (message is not null)
            {
                await ProcessWebSocketMessage(message, connection, token);
            }
        }
        catch (Exception ex) when (!IsFatal(ex))
        {
            logger.ErrorProcessingMessage(ex);
        }
    }

    async Task ProcessWebSocketMessage(ObservableQueryHubMessage message, WebSocketConnection connection, CancellationToken token)
    {
        // Any inbound message from the client counts as activity — no keep-alive needed.
        connection.KeepAliveTracker.RecordActivity();

        switch (message.Type)
        {
            case ObservableQueryHubMessageType.Subscribe:
                await HandleWebSocketSubscribe(message, connection, token);
                break;

            case ObservableQueryHubMessageType.Unsubscribe:
                HandleWebSocketUnsubscribe(message, connection.Subscriptions);
                break;

            case ObservableQueryHubMessageType.Ping:
                await SendWebSocketMessage(connection, ObservableQueryHubMessage.CreatePong(message.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), token);
                break;
        }
    }

    async Task HandleWebSocketSubscribe(ObservableQueryHubMessage message, WebSocketConnection connection, CancellationToken token)
    {
        var context = connection.Context;
        using var receipt = OperationContextScope.Begin(context.RequestServices);
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
        var operation = ReserveSubscription(connection.Subscriptions, queryId, message.Revision, token);
        if (operation is null)
        {
            return;
        }

        logger.ClientSubscribed(request.QueryName, queryId);
        var subscriptionContext = new ObservableQuerySubscriptionHttpRequestContext(context, context, serviceProvider, operation.Token);

        var session = new SubscriptionSession
        {
            SubscriptionContext = subscriptionContext,
            Subscriptions = connection.Subscriptions,
            ConnectionId = connection.ConnectionId,
            QueryId = queryId,
            Request = request,
            Operation = operation,
            Protocol = "WebSocket",
            Send = (outgoing, operationToken) => SendWebSocketMessage(connection, outgoing, operationToken)
        };

        // Establishing a subscription runs the whole query pipeline. Doing that inline would stop this connection from
        // reading anything else until it finished: every other subscribe, an unsubscribe, even a ping. A page opens
        // dozens of queries at once on its one connection, so they would run strictly one after the other and the
        // slowest query would hold up everything queued behind it. The revision was reserved above, in arrival order,
        // so running them side by side cannot reorder a replacement.
        connection.Track(EstablishWithinLimit(connection, session, context));
    }

    async Task EstablishWithinLimit(WebSocketConnection connection, SubscriptionSession session, IHttpRequestContext context)
    {
        var operation = session.Operation;
        try
        {
            await connection.Concurrency.WaitAsync(operation.Token);
        }
        catch (OperationCanceledException)
        {
            // Replaced or disconnected while waiting for a turn; EstablishSubscription never ran to release it.
            TerminateSubscription(session.Subscriptions, session.QueryId, operation);
            operation.CompleteCreation();
            return;
        }

        try
        {
            await EstablishSubscription(session, context);
        }
        catch (Exception ex) when (!IsFatal(ex))
        {
            logger.ErrorProcessingMessage(ex);
        }
        finally
        {
            connection.Concurrency.Release();
        }
    }

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

    async Task<bool> SendWebSocketMessage(WebSocketConnection connection, ObservableQueryHubMessage message, CancellationToken token)
    {
        var webSocket = connection.WebSocket;
        var writeLock = connection.WriteLock;
        var lockHeld = false;

        try
        {
            if (webSocket.State != WebSocketState.Open)
            {
                return false;
            }

            await writeLock.WaitAsync(token);
            lockHeld = true;

            return await WriteWebSocketMessage(webSocket, message, connection.KeepAliveTracker, token);
        }
        catch (Exception ex) when (
            token.IsCancellationRequested &&
            ex is OperationCanceledException or ObjectDisposedException)
        {
            // Normal shutdown or subscription cancellation.
        }
        catch (Exception ex) when (!IsFatal(ex))
        {
            logger.ErrorSendingMessage(ex);
        }
        finally
        {
            if (lockHeld)
            {
                ReleaseWriteLock(writeLock, token);
            }
        }

        return false;
    }

    async Task<bool> WriteWebSocketMessage(IWebSocket webSocket, ObservableQueryHubMessage message, KeepAliveTracker keepAliveTracker, CancellationToken token)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            return false;
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(message, arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<ObservableQueryHubMessage>());
        await webSocket.Send(new ArraySegment<byte>(json), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
        token.ThrowIfCancellationRequested();
        keepAliveTracker.RecordMessageSent();
        return true;
    }

    Task RunWebSocketKeepAlive(WebSocketConnection connection, CancellationToken token) =>
        RunKeepAlive(
            connection.KeepAliveTracker,
            () => SendWebSocketMessage(connection, ObservableQueryHubMessage.CreatePing(), token),
            token);

    sealed record WebSocketConnection(
        IWebSocket WebSocket,
        ObservableQuerySubscriptionStates Subscriptions,
        IHttpRequestContext Context,
        string ConnectionId,
        KeepAliveTracker KeepAliveTracker,
        SemaphoreSlim WriteLock)
    {
        readonly System.Collections.Concurrent.ConcurrentDictionary<Task, byte> _inFlight = new();

        /// <summary>Gets the limit on subscriptions being established at the same time on this connection.</summary>
        public SemaphoreSlim Concurrency { get; } = new(MaxConcurrentSubscriptionsPerConnection, MaxConcurrentSubscriptionsPerConnection);

        /// <summary>Remembers a subscription being established so the connection can wait for it before it is torn down.</summary>
        /// <param name="work">The work to track.</param>
        public void Track(Task work)
        {
            _inFlight[work] = 0;
            _ = work.ContinueWith(finished => _inFlight.TryRemove(finished, out _), TaskScheduler.Default);
        }

        /// <summary>Waits for every subscription still being established.</summary>
        /// <returns>A task that completes when none is left.</returns>
        public Task WhenIdle() => Task.WhenAll(_inFlight.Keys);
    }
}
