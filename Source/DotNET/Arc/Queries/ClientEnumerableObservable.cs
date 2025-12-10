// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IClientEnumerableObservable"/>.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <param name="enumerable">The <see cref="IAsyncEnumerable{T}"/> to use for streaming.</param>
/// <param name="jsonOptions">The <see cref="JsonOptions"/>.</param>
/// <param name="webSocketConnectionHandler">The <see cref="IWebSocketConnectionHandler"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientEnumerableObservable<T>(
    IAsyncEnumerable<T> enumerable,
    JsonOptions jsonOptions,
    IWebSocketConnectionHandler webSocketConnectionHandler,
    ILogger<IClientObservable> logger)
    : IClientEnumerableObservable
{
    /// <inheritdoc/>
    public async Task HandleConnection(HttpContext httpContext)
    {
        using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        using var cts = new CancellationTokenSource();
        var tsc = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new QueryResult();

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

                    queryResult.Data = item;
                    var error = await webSocketConnectionHandler.SendMessage(webSocket, queryResult, jsonOptions.JsonSerializerOptions, cts.Token, logger);
                    if (error is null)
                    {
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

        await webSocketConnectionHandler.HandleIncomingMessages(webSocket, cts.Token, logger);
        await cts.CancelAsync();
        await tsc.Task;
    }
}
