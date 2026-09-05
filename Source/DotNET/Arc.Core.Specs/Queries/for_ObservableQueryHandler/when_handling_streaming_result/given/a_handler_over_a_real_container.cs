// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.when_handling_streaming_result.given;

/// <summary>
/// The handler reaches all four observable implementations reflectively, through
/// <see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/> with a hand-written argument
/// list. An implementation whose constructor stops lining up with that list still compiles — it throws
/// <c>InvalidOperationException</c> at runtime, on the first client that asks for that transport. A substituted
/// service provider cannot catch that, because nothing is ever actually constructed, so these specs run the real
/// construction over a real container and let each connection deliver something.
/// </summary>
public class a_handler_over_a_real_container : Specification
{
    protected const string StreamingQueryName = "MyApp.Queries.StreamingQuery";

    protected IHttpRequestContext _context;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected ConcurrentQueue<string> _written;
    protected ConcurrentQueue<QueryResult> _sent;
    protected CancellationTokenSource _requestAborted;
    protected ObservableQueryHandler _handler;

    void Establish()
    {
        _written = [];
        _sent = [];
        _requestAborted = new CancellationTokenSource();

        var readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1)));

        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _webSocketConnectionHandler
            .SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _sent.Enqueue(callInfo.Arg<QueryResult>());
                return Task.FromResult<Exception?>(null);
            });

        var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        hostApplicationLifetime.ApplicationStopping.Returns(CancellationToken.None);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(readModelInterceptors);
        services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        services.AddSingleton(_webSocketConnectionHandler);
        services.AddSingleton(hostApplicationLifetime);
        services.AddSingleton(Substitute.For<IObservableQueryEmissionGuards>());
        services.AddSingleton(Options.Create(new ArcOptions()));

        var queryContextManager = Substitute.For<IQueryContextManager>();
        queryContextManager.Current.Returns(new QueryContext(StreamingQueryName, CorrelationId.New(), Paging.NotPaged, Sorting.None));

        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(_requestAborted.Token);
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _written.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });

        _handler = new ObservableQueryHandler(
            queryContextManager,
            services.BuildServiceProvider(),
            Substitute.For<ILogger<ObservableQueryHandler>>());
    }

    void Destroy() => _requestAborted.Dispose();

    /// <summary>
    /// Makes the request look like a WebSocket upgrade, so the handler takes the WebSocket branch.
    /// </summary>
    protected void ConnectOverWebSocket()
    {
        var webSocketContext = Substitute.For<IWebSocketContext>();
        webSocketContext.IsWebSocketRequest.Returns(true);
        webSocketContext.AcceptWebSocket(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Substitute.For<IWebSocket>()));
        _context.WebSockets.Returns(webSocketContext);
    }

    /// <summary>
    /// Makes the request ask for Server-Sent Events, so the handler takes the SSE branch.
    /// </summary>
    protected void ConnectOverSse()
    {
        _context.WebSockets.Returns(Substitute.For<IWebSocketContext>());
        _context.Headers.Returns(new Dictionary<string, string> { ["Accept"] = HttpRequestContextExtensions.SseContentType });
    }

    /// <summary>
    /// Two items, yielded asynchronously — the async-enumerable counterpart of a subject that has a value ready.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the connection ends.</param>
    /// <returns>The stream of items.</returns>
    protected static async IAsyncEnumerable<string> TwoItems([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in new[] { "item-a", "item-b" })
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    protected static async Task WaitFor(Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(2);
        while (DateTimeOffset.UtcNow < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }
    }
}
