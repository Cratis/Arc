// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
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
        var queryResult = new QueryResult();
        var hasDeliveredEmission = false;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in enumerable.WithCancellation(cts.Token))
                {
                    if (item is null)
                    {
                        logger.ObservableReceivedNullItem();
                        continue;
                    }

                    // Resolve through the connection's own request-scoped provider — captured here via closure —
                    // rather than the root, so the interceptor releases compliance/PII data under this
                    // subscription's tenant instead of whichever tenant happened to resolve first.
                    queryResult.Data = await readModelInterceptors.InterceptEmission(typeof(T), item, context.RequestServices);

                    if (emissionGuards.HasGuards)
                    {
                        var verdict = await emissionGuards.Guard(new ObservableQueryEmissionContext(
                            queryContext.Name,
                            queryContext.Arguments ?? QueryArguments.Empty,
                            context.User,
                            queryContext.CorrelationId,
                            context.RequestServices,
                            !hasDeliveredEmission,
                            cts.Token));

                        if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
                        {
                            logger.ObservableEmissionDenied();

                            // Send the terminal unauthorized result before the stream goes away — a client that only
                            // sees the socket close reads it as a transport hiccup and reconnects into the same denial.
                            await webSocketConnectionHandler.SendMessage(webSocket, QueryResult.Unauthorized(queryContext.CorrelationId), writeLock, cts.Token);
                            break;
                        }

                        if (verdict == ObservableQueryEmissionVerdict.Suppress)
                        {
                            logger.ObservableEmissionSuppressed();
                            continue;
                        }
                    }

                    var error = await webSocketConnectionHandler.SendMessage(webSocket, queryResult, writeLock, cts.Token);
                    if (error is null)
                    {
                        hasDeliveredEmission = true;
                        continue;
                    }
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }
                    logger.EnumerableObservableSkip();
                }
                tsc.SetResult();
                await cts.CancelAsync();
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    logger.EnumerableObservableError(ex);
                    await cts.CancelAsync();
                    tsc.SetResult();
                }
            }
        });

        await webSocketConnectionHandler.HandleIncomingMessages(webSocket, writeLock, cts.Token);
        await cts.CancelAsync();
        await tsc.Task;
    }
}
