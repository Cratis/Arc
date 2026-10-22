// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

public partial class ObservableQueryDemultiplexer
{
    /// <inheritdoc/>
    public async Task HandleSSEConnection(IHttpRequestContext context)
    {
        var connectionId = Guid.NewGuid().ToString();

        logger.SseClientConnected(connectionId);

        using var request = HttpRequestContextAccessor.BeginRequest(httpRequestContextAccessor, context);

        context.SetSseResponseHeaders();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            hostApplicationLifetime.ApplicationStopping);

        var state = new SSEConnectionState(context, linkedCts, ObservableQueryCaller.CapturedFrom(context));
        _sseConnections[connectionId] = state;

#pragma warning disable CA2025 // keepAliveTask is always awaited in the finally block before linkedCts is disposed
        var keepAliveTask = RunSseKeepAlive(state);
#pragma warning restore CA2025

        try
        {
            // Send the Connected message so the client knows its connection ID for POST requests, and the
            // keep-alive interval it should expect messages on.
            await SendSseMessage(
                state,
                ObservableQueryHubMessage.CreateConnected(connectionId, arcOptions.Value.Query.KeepAliveInterval),
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
        using var receipt = OperationContextScope.Begin(context.RequestServices);
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

        if (!IsValidSubscribeRequest(body))
        {
            context.SetStatusCode(400);
            return;
        }

        var state = ResolveOwnedConnection(context, body.ConnectionId);
        if (state is null)
        {
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

        var session = new SubscriptionSession
        {
            SubscriptionContext = subscriptionContext,
            Subscriptions = state.Subscriptions,
            ConnectionId = body.ConnectionId,
            QueryId = body.QueryId,
            Request = body.Request,
            Operation = operation,
            Protocol = "SSE",
            Send = (message, operationToken) => SendSseMessage(state, message, operationToken)
        };
        var wasUnauthorized = await EstablishSubscription(session, context);

        context.SetStatusCode(wasUnauthorized ? 401 : 200);
    }

    /// <inheritdoc/>
    public async Task HandleSSEUnsubscribe(IHttpRequestContext context)
    {
        var body = await context.ReadBodyAsJson(typeof(ObservableQuerySSEUnsubscribeRequest), context.RequestAborted)
            as ObservableQuerySSEUnsubscribeRequest;
        if (!IsValidUnsubscribeRequest(body))
        {
            context.SetStatusCode(400);
            return;
        }

        var state = ResolveOwnedConnection(context, body.ConnectionId);
        if (state is null)
        {
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

    static bool IsValidUnsubscribeRequest([NotNullWhen(true)] ObservableQuerySSEUnsubscribeRequest? body) =>
        body is not null
        && !string.IsNullOrEmpty(body.ConnectionId)
        && !string.IsNullOrEmpty(body.QueryId)
        && ObservableQuerySubscriptionRevision.IsValid(body.Revision);

    static bool IsValidSubscribeRequest([NotNullWhen(true)] ObservableQuerySSESubscribeRequest? body) =>
        body is not null
        && !string.IsNullOrEmpty(body.ConnectionId)
        && !string.IsNullOrEmpty(body.QueryId)
        && body.Request is not null
        && ObservableQuerySubscriptionRevision.IsValid(body.Revision);

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

    async Task<bool> SendSseMessage(SSEConnectionState state, ObservableQueryHubMessage message, CancellationToken operationToken)
    {
        var writeLock = state.WriteLock;
        var cts = state.CancellationTokenSource;
        var writeLockHeld = false;

        try
        {
            await writeLock.WaitAsync(operationToken);
            writeLockHeld = true;

            var json = JsonSerializer.Serialize(message, arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<ObservableQueryHubMessage>());
            await state.Context.Write($"data: {json}\n\n", operationToken);
            operationToken.ThrowIfCancellationRequested();
            state.KeepAliveTracker.RecordMessageSent();
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
        catch (Exception ex) when (!IsFatal(ex))
        {
            logger.ErrorSendingMessage(ex);
        }
        finally
        {
            if (writeLockHeld)
            {
                ReleaseWriteLock(writeLock, operationToken);
            }
        }

        return false;
    }

    Task RunSseKeepAlive(SSEConnectionState state) =>
        RunKeepAlive(
            state.KeepAliveTracker,
            () => SendSseMessage(state, ObservableQueryHubMessage.CreatePing(), state.CancellationTokenSource.Token),
            state.CancellationTokenSource.Token);

    /// <summary>
    /// Resolves the SSE connection <paramref name="connectionId"/> names, but only for the caller that opened it.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the control request.</param>
    /// <param name="connectionId">The connection identifier carried in the control request body.</param>
    /// <returns>The <see cref="SSEConnectionState"/>, or null when there is no such connection or it belongs to another caller.</returns>
    /// <remarks>
    /// Both outcomes are deliberately indistinguishable to the caller, so that probing connection identifiers
    /// reveals nothing about which ones exist.
    /// </remarks>
    SSEConnectionState? ResolveOwnedConnection(IHttpRequestContext context, string connectionId)
    {
        if (!_sseConnections.TryGetValue(connectionId, out var state))
        {
            logger.SseUnknownConnection(connectionId);
            return null;
        }

        if (!state.Caller.IsSameCallerAs(ObservableQueryCaller.CapturedFrom(context)))
        {
            logger.SseConnectionNotOwnedByCaller(connectionId);
            return null;
        }

        return state;
    }

    sealed record SSEConnectionState(
        IHttpRequestContext Context,
        CancellationTokenSource CancellationTokenSource,
        ObservableQueryCaller Caller)
    {
        public ObservableQuerySubscriptionStates Subscriptions { get; } = new();
        public CancellationToken CancellationToken { get; } = CancellationTokenSource.Token;
        public KeepAliveTracker KeepAliveTracker { get; } = new();
        public SemaphoreSlim WriteLock { get; } = new(1, 1);
    }
}
