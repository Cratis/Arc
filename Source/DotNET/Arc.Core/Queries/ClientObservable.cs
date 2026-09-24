// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
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
        var drain = new DirectObservableEmissionDrain(cts);
        var emissionTenant = queryContext.EmissionTenant ?? context.RequestServices.GetService<TenantIdAccessor>()?.Current;
        var nativeRequest = (context.RequestServices.GetService<IAuthorizationPolicyRuntime>() as IAuthorizationEmissionRuntime)?
            .CaptureLiveRequest(context.RequestServices);
        using var receiveCts = new CancellationTokenSource();
        using var writeLock = new SemaphoreSlim(1, 1);
        using var emissionGate = new SemaphoreSlim(1, 1);

        IDisposable? subscription = null;
        var receiveTask = Task.CompletedTask;
        var receiverWatch = Task.CompletedTask;
        Exception? failure = null;
        Exception? terminationFailure = null;
        Exception? receiverFailure = null;
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping, context.RequestAborted);
        await using var disconnectRegistration = linkedTokenSource.Token.Register(() =>
        {
            _ = drain.Cancel();
            tcs.TrySetResult();
        });
        try
        {
            subscription = Subject.Subscribe(Next, Error, Complete);
            receiveTask = Receive();
            receiverWatch = receiveTask.ContinueWith(
                completed =>
                {
                    _ = drain.Cancel();
                    tcs.TrySetResult();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            await tcs.Task;
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            // Completion does not cancel writes: accepted callbacks finish before the socket and request scope
            // are released. On any failure, cancellation and unsubscription may fail too, but cannot skip the drain.
            try
            {
                await drain.TerminateAsync(subscription, failure, receiveCts.CancelAsync);
            }
            catch (Exception error)
            {
                terminationFailure = error;
            }

            try
            {
                await receiveTask;
            }
            catch (Exception error)
            {
                receiverFailure = error;
            }

            await receiverWatch;
        }

        if (terminationFailure is not null && receiverFailure is not null)
        {
            throw new ObservableQueryTeardownFailed([terminationFailure, receiverFailure]);
        }

        if (terminationFailure is not null || receiverFailure is not null)
        {
            ExceptionDispatchInfo.Capture(terminationFailure ?? receiverFailure).Throw();
        }

        return;

        async Task Receive() => await webSocketConnectionHandler.HandleIncomingMessages(webSocket, writeLock, receiveCts.Token);

        async void Next(T data)
        {
            if (!drain.TryEnter())
            {
                return;
            }

            var gateHeld = false;
            try
            {
                await emissionGate.WaitAsync(cts.Token);
                gateHeld = true;
                if (cts.IsCancellationRequested || isTerminated)
                {
                    return;
                }

                using var emissionIdentity = context is ObservableQuerySubscriptionHttpRequestContext selectedSubscription
                    ? selectedSubscription.BeginEmission()
                    : ObservableEmissionIdentity.Begin(context, context.RequestServices, context.User, emissionTenant, queryContext.NativeEmissionRequest ?? nativeRequest, httpRequestContextAccessor);
                if (data is null)
                {
                    // A single-document observable emits default/null to report "no such document" (removed,
                    // updated out of the filter, or never found) rather than completing the observable — the
                    // emission is forwarded below like any other, not dropped.
                    logger.ObservableForwardingNullItem();
                }

                queryResult.Paging = new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems);

                // Next() is invoked by the subject's producer on its own thread, where the AsyncLocal tenant
                // context set up for this connection does not flow. Restoring it here — and resolving through
                // the connection's own request-scoped provider rather than the root — ensures the interceptor
                // releases compliance/PII data under this subscription's tenant, not whichever tenant happened
                // to resolve first.
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
            finally
            {
                if (gateHeld)
                {
                    emissionGate.Release();
                }

                drain.Exit();
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
                _ = drain.Cancel();
                tcs.TrySetResult();
            }
        }
        void Complete()
        {
            if (!cts.IsCancellationRequested)
            {
                logger.ObservableCompleted();
            }
            tcs.TrySetResult();
        }
    }
}
