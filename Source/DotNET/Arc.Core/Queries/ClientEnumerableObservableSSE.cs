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
/// <param name="enumerable">The <see cref="IAsyncEnumerable{T}"/> to use for streaming.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve interceptors.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientEnumerableObservableSSE<T>(
    IAsyncEnumerable<T> enumerable,
    IReadModelInterceptors readModelInterceptors,
    IServiceProvider serviceProvider,
    IOptions<ArcOptions> arcOptions,
    ILogger<IClientObservable> logger)
    : IClientEnumerableObservable
{
    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context)
    {
        context.SetSseResponseHeaders();

        using var cts = new CancellationTokenSource();
        var queryResult = new QueryResult();

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

                await readModelInterceptors.Intercept(typeof(T), item, serviceProvider);

                queryResult.Data = item;
                var json = JsonSerializer.Serialize(queryResult, arcOptions.Value.JsonSerializerOptions);
                var sseMessage = $"data: {json}\n\n";

                try
                {
                    await context.Write(sseMessage, linkedCts.Token);
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
