// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IClientObservable"/>.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientObservable{T}"/> class.
/// </remarks>
/// <param name="queryContext">The <see cref="QueryContext"/> the observable is for.</param>
/// <param name="subject">The <see cref="ISubject{T}"/> the observable wraps.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> for intercepting read models.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve interceptors.</param>
/// <param name="webSocketConnectionHandler">The <see cref="IWebSocketConnectionHandler"/>.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientObservable<T>(
    QueryContext queryContext,
    ISubject<T> subject,
    IReadModelInterceptors readModelInterceptors,
    IServiceProvider serviceProvider,
    IWebSocketConnectionHandler webSocketConnectionHandler,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<ClientObservable<T>> logger) : ClientObservableBase<T>(subject)
{
    /// <summary>
    /// Notifies all subscribed and future observers about the arrival of the specified element in the sequence.
    /// </summary>
    /// <param name="next">The value to send to all observers.</param>
    public void OnNext(T next) => Subject.OnNext(next);

    /// <inheritdoc/>
    protected override async Task HandleConnectionCore(IHttpRequestContext context)
    {
        var webSocket = await context.WebSockets.AcceptWebSocket();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new QueryResult();
        using var cts = new CancellationTokenSource();
        using var writeLock = new SemaphoreSlim(1, 1);

        using var subscription = Subject.Subscribe(Next, Error, Complete);

        // If application is stopping, complete the observable
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, hostApplicationLifetime.ApplicationStopping);
        linkedTokenSource.Token.Register(Complete);

        await webSocketConnectionHandler.HandleIncomingMessages(webSocket, writeLock, cts.Token);

        // The client disconnected — clean up without completing the shared subject.
        if (!cts.IsCancellationRequested)
        {
            await cts.CancelAsync();
        }

        tcs.TrySetResult();

        await tcs.Task;
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

                var intercepted = await readModelInterceptors.Intercept(typeof(T), [data], serviceProvider);

                queryResult.Paging = new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems);
                queryResult.Data = intercepted.First();

                var error = await webSocketConnectionHandler.SendMessage(webSocket, queryResult, writeLock, cts.Token);
                if (error is not null && !cts.IsCancellationRequested)
                {
                    Subject.OnError(error);
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
            logger.ObservableAnErrorOccurred(error);
            if (!cts.IsCancellationRequested)
            {
                _ = cts.CancelAsync();
                tcs.TrySetResult();
            }
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
