// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IClientObservable"/> that uses Server-Sent Events (SSE) for streaming.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientObservableSSE{T}"/> class.
/// </remarks>
/// <param name="queryContext">The <see cref="QueryContext"/> the observable is for.</param>
/// <param name="subject">The <see cref="ISubject{T}"/> the observable wraps.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve interceptors.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientObservableSSE<T>(
    QueryContext queryContext,
    ISubject<T> subject,
    IReadModelInterceptors readModelInterceptors,
    IServiceProvider serviceProvider,
    IOptions<ArcOptions> arcOptions,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<ClientObservableSSE<T>> logger) : ClientObservableBase<T>(subject)
{
    /// <inheritdoc/>
    protected override async Task HandleConnectionCore(IHttpRequestContext context)
    {
        context.SetSseResponseHeaders();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new QueryResult();
        using var cts = new CancellationTokenSource();

        using var subscription = Subject.Subscribe(Next, Error, Complete);

        // If application is stopping, complete the observable
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, hostApplicationLifetime.ApplicationStopping, context.RequestAborted);
        linkedTokenSource.Token.Register(Complete);

        try
        {
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        finally
        {
            await cts.CancelAsync();
        }

        return;

        async void Next(T data)
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (data is null)
                {
                    logger.ObservableReceivedNullItem();
                    return;
                }

                await readModelInterceptors.Intercept(typeof(T), data, serviceProvider);

                queryResult.Paging = new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems);
                queryResult.Data = data;

                var json = JsonSerializer.Serialize(queryResult, arcOptions.Value.JsonSerializerOptions);
                var sseMessage = $"data: {json}\n\n";

                try
                {
                    await context.Write(sseMessage, cts.Token);
                }
                catch (Exception ex)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        Subject.OnError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    Subject.OnError(ex);
                }
            }
        }

        void Error(Exception error)
        {
            if (cts.IsCancellationRequested)
            {
                Complete();
                return;
            }
            logger.ObservableAnErrorOccurred(error);
            Complete();
        }

        void Complete()
        {
            if (!cts.IsCancellationRequested)
            {
                logger.ObservableCompleted();
                _ = cts.CancelAsync();
            }
            tcs.TrySetResult();
        }
    }
}
