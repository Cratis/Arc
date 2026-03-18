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
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientEnumerableObservableSSE<T>(
    IAsyncEnumerable<T> enumerable,
    IOptions<ArcOptions> arcOptions,
    ILogger<IClientObservable> logger)
    : IClientEnumerableObservable
{
    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context)
    {
        context.ContentType = $"{ClientObservableSSE<T>.ContentType}; charset=utf-8";
        context.SetResponseHeader("Cache-Control", "no-cache");
        context.SetResponseHeader("Connection", "keep-alive");
        context.SetResponseHeader("X-Accel-Buffering", "no");

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
