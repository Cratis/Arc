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
/// <param name="webSocketConnectionHandler">The <see cref="IWebSocketConnectionHandler"/>.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientObservable<T>(
    QueryContext queryContext,
    ISubject<T> subject,
    IWebSocketConnectionHandler webSocketConnectionHandler,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<ClientObservable<T>> logger) : IClientObservable, IAsyncEnumerable<T>
{
    /// <summary>
    /// Notifies all subscribed and future observers about the arrival of the specified element in the sequence.
    /// </summary>
    /// <param name="next">The value to send to all observers.</param>
    public void OnNext(T next) => subject.OnNext(next);

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new ObservableAsyncEnumerator<T>(subject, cancellationToken);

    /// <inheritdoc/>
    public object GetAsynchronousEnumerator(CancellationToken cancellationToken = default) => GetAsyncEnumerator(cancellationToken);

    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context) =>
        await HandleConnectionCore(context);

    async Task HandleConnectionCore(IHttpRequestContext context)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new QueryResult();
        using var cts = new CancellationTokenSource();

        using var subscription = subject.Subscribe(Next, Error, Complete);

        // If application is stopping, complete the observable
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, hostApplicationLifetime.ApplicationStopping);
        linkedTokenSource.Token.Register(Complete);

        await webSocketConnectionHandler.HandleIncomingMessages(webSocket, cts.Token);
        subject.OnCompleted();

        await tcs.Task;
        return;

        async void Next(T data)
        {
            try
            {
                if (data is null)
                {
                    logger.ObservableReceivedNullItem();
                    return;
                }

                queryResult.Paging = new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems);
                queryResult.Data = data;

                var error = await webSocketConnectionHandler.SendMessage(webSocket, queryResult, cts.Token);
                if (error is not null)
                {
                    subject.OnError(error);
                }
            }
            catch (Exception ex)
            {
                subject.OnError(ex);
            }
        }
        void Error(Exception error)
        {
            if (cts.IsCancellationRequested)
            {
                subject.OnCompleted();
            }
            logger.ObservableAnErrorOccurred(error);
        }
        void Complete()
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }
            logger.ObservableCompleted();
            cts.Cancel();
            tcs.SetResult();
        }
    }
}
