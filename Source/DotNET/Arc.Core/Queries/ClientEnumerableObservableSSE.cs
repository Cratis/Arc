// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;
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
        var queryResult = new QueryResult();
        var hasDeliveredEmission = false;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, context.RequestAborted);

        try
        {
            await foreach (var item in enumerable.WithCancellation(linkedCts.Token))
            {
                if (item is null)
                {
                    logger.ObservableReceivedNullItem();
                    continue;
                }

                // Resolve through the connection's own request-scoped provider rather than the root, so the
                // interceptor releases compliance/PII data under this subscription's tenant instead of
                // whichever tenant happened to resolve first.
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
                        linkedCts.Token));

                    if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
                    {
                        logger.ObservableEmissionDenied();

                        // Send the terminal unauthorized result before the stream goes away — a client that only sees
                        // the stream end reads it as a transport hiccup and reconnects into the same denial.
                        var unauthorizedJson = JsonSerializer.Serialize(QueryResult.Unauthorized(queryContext.CorrelationId), arcOptions.Value.JsonSerializerOptions);
                        await context.Write($"data: {unauthorizedJson}\n\n", linkedCts.Token);
                        break;
                    }

                    if (verdict == ObservableQueryEmissionVerdict.Suppress)
                    {
                        logger.ObservableEmissionSuppressed();
                        continue;
                    }
                }

                var json = JsonSerializer.Serialize(queryResult, arcOptions.Value.JsonSerializerOptions);
                var sseMessage = $"data: {json}\n\n";

                try
                {
                    await context.Write(sseMessage, linkedCts.Token);
                    hasDeliveredEmission = true;
                }
                catch (Exception ex)
                {
                    if (!linkedCts.IsCancellationRequested)
                    {
                        logger.EnumerableObservableError(ex);
                    }
                    break;
                }
            }
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
}
