// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Observability;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IClientEnumerableObservable"/>.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <param name="queryContext">The <see cref="QueryContext"/> the observable is for.</param>
/// <param name="enumerable">The <see cref="IAsyncEnumerable{T}"/> to use for streaming.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="webSocketConnectionHandler">The <see cref="IWebSocketConnectionHandler"/>.</param>
/// <param name="emissionGuards">The <see cref="IObservableQueryEmissionGuards"/> consulted per emission when an application opts in with an <see cref="IGuardObservableQueryEmission"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientEnumerableObservable<T>(
    QueryContext queryContext,
    IAsyncEnumerable<T> enumerable,
    IReadModelInterceptors readModelInterceptors,
    IWebSocketConnectionHandler webSocketConnectionHandler,
    IObservableQueryEmissionGuards emissionGuards,
    ILogger<IClientObservable> logger)
    : IClientEnumerableObservable
{
    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context)
    {
        var webSocket = await context.WebSockets.AcceptWebSocket();
        using var cts = new CancellationTokenSource();
        using var writeLock = new SemaphoreSlim(1, 1);
        var tsc = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var telemetry = new ObservableQueryTelemetry(context.RequestServices, queryContext.Name.Value, WellKnownTelemetryNames.WebSocketTransport);
        telemetry.Start();
        var connection = new Connection(context, webSocket, cts, writeLock, telemetry);

        _ = Task.Run(async () =>
        {
            try
            {
                await Stream(connection);
                await cts.CancelAsync();
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    logger.EnumerableObservableError(ex);
                    await cts.CancelAsync();
                }
            }
            finally
            {
                // Complete on every path, including cancellation after the client disconnected. Otherwise
                // HandleConnection never returns and the subscription it holds is never released.
                tsc.TrySetResult();
            }
        });

        await webSocketConnectionHandler.HandleIncomingMessages(webSocket, writeLock, cts.Token);
        await cts.CancelAsync();
        await tsc.Task;
    }

    IDisposable? BeginEmissionIdentity(IHttpRequestContext context) =>
        context is ObservableQuerySubscriptionHttpRequestContext subscription
            ? subscription.BeginEmission()
            : queryContext.EmissionTenant is not null || queryContext.NativeEmissionRequest is not null
                ? ObservableEmissionIdentity.Begin(context, context.RequestServices, context.User, queryContext.EmissionTenant, queryContext.NativeEmissionRequest)
                : null;

    async Task Stream(Connection connection)
    {
        using var emissionIdentity = BeginEmissionIdentity(connection.Context);
        await foreach (var item in enumerable.WithCancellation(connection.Cancellation.Token))
        {
            if (!await Deliver(connection, item))
            {
                break;
            }
        }
    }

    async Task<bool> Deliver(Connection connection, T item)
    {
        if (item is null)
        {
            logger.ObservableReceivedNullItem();
            return true;
        }

        // Resolve through the connection's own request-scoped provider — captured here via closure —
        // rather than the root, so the interceptor releases compliance/PII data under this
        // subscription's tenant instead of whichever tenant happened to resolve first.
        connection.Result.Data = await readModelInterceptors.InterceptEmission(typeof(T), item, connection.Context.RequestServices);

        if (emissionGuards.HasGuards)
        {
            var verdict = await GuardEmission(connection);
            if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
            {
                logger.ObservableEmissionDenied();

                // Send the terminal unauthorized result before the stream goes away — a client that only
                // sees the socket close reads it as a transport hiccup and reconnects into the same denial.
                await webSocketConnectionHandler.SendMessage(connection.WebSocket, QueryResult.Unauthorized(queryContext.CorrelationId), connection.WriteLock, connection.Cancellation.Token);
                return false;
            }

            if (verdict == ObservableQueryEmissionVerdict.Suppress)
            {
                logger.ObservableEmissionSuppressed();
                return true;
            }
        }

        return await Push(connection);
    }

    Task<ObservableQueryEmissionVerdict> GuardEmission(Connection connection) =>
        emissionGuards.Guard(new ObservableQueryEmissionContext(
            queryContext.Name,
            queryContext.Arguments ?? QueryArguments.Empty,
            connection.Context.User,
            queryContext.CorrelationId,
            connection.Context.RequestServices,
            !connection.HasDeliveredEmission,
            connection.Cancellation.Token)
        {
            SubscriptionScopeSnapshot = queryContext.SubscriptionScopeSnapshot
        });

    async Task<bool> Push(Connection connection)
    {
        using var push = connection.Telemetry.BeginPush();
        var result = await WebSocketSends.Send(webSocketConnectionHandler, connection.WebSocket, connection.Result, connection.WriteLock, connection.Cancellation.Token);
        if (result.Status == WebSocketSendStatus.Sent)
        {
            push.Delivered();
            connection.HasDeliveredEmission = true;
            return true;
        }

        if (result.Error is null)
        {
            return true;
        }

        if (connection.Cancellation.IsCancellationRequested)
        {
            return false;
        }

        logger.EnumerableObservableSkip();
        return true;
    }

    sealed class Connection(IHttpRequestContext context, IWebSocket webSocket, CancellationTokenSource cancellation, SemaphoreSlim writeLock, ObservableQueryTelemetry telemetry)
    {
        public IHttpRequestContext Context { get; } = context;

        public IWebSocket WebSocket { get; } = webSocket;

        public CancellationTokenSource Cancellation { get; } = cancellation;

        public SemaphoreSlim WriteLock { get; } = writeLock;

        public ObservableQueryTelemetry Telemetry { get; } = telemetry;

        public QueryResult Result { get; } = new();

        public bool HasDeliveredEmission { get; set; }
    }
}
