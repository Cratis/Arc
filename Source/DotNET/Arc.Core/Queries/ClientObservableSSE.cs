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
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> restored around each emission so tenant resolution sees the subscribing connection, not whatever ambient context the emitting thread happens to carry.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
/// <param name="emissionGuards">The <see cref="IObservableQueryEmissionGuards"/> consulted per emission when an application opts in with an <see cref="IGuardObservableQueryEmission"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientObservableSSE<T>(
    QueryContext queryContext,
    ISubject<T> subject,
    IReadModelInterceptors readModelInterceptors,
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IOptions<ArcOptions> arcOptions,
    IHostApplicationLifetime hostApplicationLifetime,
    IObservableQueryEmissionGuards emissionGuards,
    ILogger<ClientObservableSSE<T>> logger) : ClientObservableBase<T>(subject)
{
    /// <inheritdoc/>
    protected override async Task HandleConnectionCore(IHttpRequestContext context)
    {
        context.SetSseResponseHeaders();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new QueryResult();
        var hasDeliveredEmission = false;
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

                var json = JsonSerializer.Serialize(queryResult, arcOptions.Value.JsonSerializerOptions);
                var sseMessage = $"data: {json}\n\n";

                try
                {
                    await context.Write(sseMessage, cts.Token);
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
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    Subject.OnError(ex);
                }
            }
        }

        // Returns true when the emission may be written. A denial also tells the client it is no longer authorized
        // and ends the stream; a suppression only withholds this one emission and leaves the stream running.
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

                // Send the terminal unauthorized result before the stream goes away — a client that only sees the
                // stream end reads it as a transport hiccup and reconnects straight into the same denial.
                var unauthorizedJson = JsonSerializer.Serialize(QueryResult.Unauthorized(queryContext.CorrelationId), arcOptions.Value.JsonSerializerOptions);
                await context.Write($"data: {unauthorizedJson}\n\n", cts.Token);
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
