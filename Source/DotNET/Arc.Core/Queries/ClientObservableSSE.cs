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
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class ClientObservableSSE<T>(
    QueryContext queryContext,
    ISubject<T> subject,
    IOptions<ArcOptions> arcOptions,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<ClientObservableSSE<T>> logger) : IClientObservable, IAsyncEnumerable<T>
{
    /// <summary>
    /// The SSE content type.
    /// </summary>
    public const string ContentType = "text/event-stream";

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new ObservableAsyncEnumerator<T>(subject, cancellationToken);

    /// <inheritdoc/>
    public object GetAsynchronousEnumerator(CancellationToken cancellationToken = default) => GetAsyncEnumerator(cancellationToken);

    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context) =>
        await HandleConnectionCore(context);

    async Task HandleConnectionCore(IHttpRequestContext context)
    {
        context.ContentType = $"{ContentType}; charset=utf-8";
        context.SetResponseHeader("Cache-Control", "no-cache");
        context.SetResponseHeader("Connection", "keep-alive");
        context.SetResponseHeader("X-Accel-Buffering", "no");

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new QueryResult();
        using var cts = new CancellationTokenSource();

        using var subscription = subject.Subscribe(Next, Error, Complete);

        // If application is stopping, complete the observable
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, hostApplicationLifetime.ApplicationStopping, context.RequestAborted);
        linkedTokenSource.Token.Register(Complete);

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

                var json = JsonSerializer.Serialize(queryResult, arcOptions.Value.JsonSerializerOptions);
                var sseMessage = $"data: {json}\n\n";

                try
                {
                    await context.Write(sseMessage, cts.Token);
                }
                catch (Exception ex)
                {
                    subject.OnError(ex);
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
                Complete();
                return;
            }
            logger.ObservableAnErrorOccurred(error);
            Complete();
        }

        void Complete()
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }
            logger.ObservableCompleted();
            cts.Cancel();
            tcs.TrySetResult();
        }
    }
}
