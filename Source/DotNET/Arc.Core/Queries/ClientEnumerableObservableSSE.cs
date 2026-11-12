// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Arc.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IClientEnumerableObservable"/> that uses Server-Sent Events (SSE) for streaming.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientEnumerableObservableSSE{T}"/> class.
/// </remarks>
/// <param name="queryContext">The <see cref="QueryContext"/> the observable is for.</param>
/// <param name="enumerable">The <see cref="IAsyncEnumerable{T}"/> to use for streaming.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="emissionGuards">The <see cref="IObservableQueryEmissionGuards"/> consulted per emission when an application opts in with an <see cref="IGuardObservableQueryEmission"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientEnumerableObservableSSE<T>(
    QueryContext queryContext,
    IAsyncEnumerable<T> enumerable,
    IReadModelInterceptors readModelInterceptors,
    IOptions<ArcOptions> arcOptions,
    IObservableQueryEmissionGuards emissionGuards,
    ILogger<IClientObservable> logger)
    : IClientEnumerableObservable
{
    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context)
    {
        context.SetSseResponseHeaders();

        using var cts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, context.RequestAborted);
        using var telemetry = new ObservableQueryTelemetry(context.RequestServices, queryContext.Name.Value, WellKnownTelemetryNames.ServerSentEventsTransport);
        telemetry.Start();
        var connection = new Connection(context, linkedCts, telemetry);

        try
        {
            await Stream(connection);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or server is stopping
        }
        catch (Exception ex)
        {
            logger.EnumerableObservableError(ex);
        }
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

        // Resolve through the connection's own request-scoped provider rather than the root, so the
        // interceptor releases compliance/PII data under this subscription's tenant instead of
        // whichever tenant happened to resolve first.
        connection.Result.Data = await readModelInterceptors.InterceptEmission(typeof(T), item, connection.Context.RequestServices);

        if (emissionGuards.HasGuards)
        {
            var verdict = await GuardEmission(connection);
            if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
            {
                logger.ObservableEmissionDenied();
                await SendUnauthorized(connection);
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

    async Task SendUnauthorized(Connection connection)
    {
        // Send the terminal unauthorized result before the stream goes away — a client that only sees
        // the stream end reads it as a transport hiccup and reconnects into the same denial.
        var unauthorizedJson = JsonSerializer.Serialize(QueryResult.Unauthorized(queryContext.CorrelationId), arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<QueryResult>());
        await connection.Context.Write($"data: {unauthorizedJson}\n\n", connection.Cancellation.Token);
    }

    async Task<bool> Push(Connection connection)
    {
        var json = JsonSerializer.Serialize(connection.Result, arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<QueryResult>());
        var sseMessage = $"data: {json}\n\n";

        using var push = connection.Telemetry.BeginPush();
        try
        {
            await connection.Context.Write(sseMessage, connection.Cancellation.Token);
            push.Delivered();
            connection.HasDeliveredEmission = true;
            return true;
        }
        catch (Exception ex)
        {
            if (!connection.Cancellation.IsCancellationRequested)
            {
                logger.EnumerableObservableError(ex);
            }

            return false;
        }
    }

    sealed class Connection(IHttpRequestContext context, CancellationTokenSource cancellation, ObservableQueryTelemetry telemetry)
    {
        public IHttpRequestContext Context { get; } = context;

        public CancellationTokenSource Cancellation { get; } = cancellation;

        public ObservableQueryTelemetry Telemetry { get; } = telemetry;

        public QueryResult Result { get; } = new();

        public bool HasDeliveredEmission { get; set; }
    }
}
