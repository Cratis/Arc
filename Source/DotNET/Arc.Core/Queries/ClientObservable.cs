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
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> restored around each emission so tenant resolution sees the subscribing connection, not whatever ambient context the emitting thread happens to carry.</param>
/// <param name="webSocketConnectionHandler">The <see cref="IWebSocketConnectionHandler"/>.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
/// <param name="emissionGuards">The <see cref="IObservableQueryEmissionGuards"/> consulted per emission when an application opts in with an <see cref="IGuardObservableQueryEmission"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientObservable<T>(
    QueryContext queryContext,
    ISubject<T> subject,
    IReadModelInterceptors readModelInterceptors,
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IWebSocketConnectionHandler webSocketConnectionHandler,
    IHostApplicationLifetime hostApplicationLifetime,
    IObservableQueryEmissionGuards emissionGuards,
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
        var hasDeliveredEmission = false;
        var isTerminated = false;
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
            if (cts.IsCancellationRequested || isTerminated)
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

                queryResult.Paging = new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems);

                // Next() is invoked by the subject's producer on its own thread, where the AsyncLocal tenant
                // context set up for this connection does not flow. Restoring it here — and resolving through
                // the connection's own request-scoped provider rather than the root — ensures the interceptor
                // releases compliance/PII data under this subscription's tenant, not whichever tenant happened
                // to resolve first.
                httpRequestContextAccessor.Current = context;
                queryResult.Data = await readModelInterceptors.InterceptEmission(typeof(T), data, context.RequestServices);

                if (emissionGuards.HasGuards && !await IsEmissionAllowed())
                {
                    return;
                }

                // A guard evaluating a concurrent emission may have terminated the connection while this one was
                // being intercepted and evaluated. The terminal unauthorized frame has already gone out, so nothing
                // may be written behind it.
                if (isTerminated)
                {
                    return;
                }

                var error = await webSocketConnectionHandler.SendMessage(webSocket, queryResult, writeLock, cts.Token);
                if (error is not null)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        Subject.OnError(error);
                    }

                    return;
                }

                // Only a write that actually reached the client counts as delivered — a guard asking whether this is
                // the first emission must not be told the client already has one it never received.
                hasDeliveredEmission = true;
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    Subject.OnError(ex);
                }
            }
        }

        // Returns true when the emission may be written. A denial also tells the client it is no longer authorized
        // and ends the connection; a suppression only withholds this one emission and leaves the stream running.
        async Task<bool> IsEmissionAllowed()
        {
            var verdict = await emissionGuards.Guard(new ObservableQueryEmissionContext(
                queryContext.Name,
                queryContext.Arguments ?? QueryArguments.Empty,
                context.User,
                queryContext.CorrelationId,
                context.RequestServices,
                !hasDeliveredEmission,
                cts.Token));

            if (verdict == ObservableQueryEmissionVerdict.Allow)
            {
                return true;
            }

            if (verdict == ObservableQueryEmissionVerdict.DenyAndTerminate)
            {
                logger.ObservableEmissionDenied();
                isTerminated = true;

                // Send the terminal unauthorized result before the stream goes away — a client that only sees the
                // socket close reads it as a transport hiccup and reconnects straight into the same denial.
                await webSocketConnectionHandler.SendMessage(webSocket, QueryResult.Unauthorized(queryContext.CorrelationId), writeLock, cts.Token);
                Complete();
            }
            else
            {
                logger.ObservableEmissionSuppressed();
            }

            return false;
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
