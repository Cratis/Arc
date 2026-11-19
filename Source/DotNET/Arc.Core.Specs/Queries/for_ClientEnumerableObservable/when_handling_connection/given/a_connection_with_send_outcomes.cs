// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Cratis.Arc.Http;
using Cratis.Arc.Observability;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection.given;

public class a_connection_with_send_outcomes : Specification
{
    protected readonly InvalidOperationException _error = new("Send failed");
    protected IWebSocket _socket;
    protected ILogger<IClientObservable> _logger;
    protected int _sends;
    protected int _itemsRead;
    protected bool _enumeratorDisposed;
    protected CancellationToken _sendToken;
    internal ObservedQueryServices _observed;

    protected async Task Run(WebSocketSendResult outcome, bool legacy = false, bool throws = false)
    {
        const string queryName = "SendOutcomes";
        _observed = new ObservedQueryServices([queryName]);
        _socket = Substitute.For<IWebSocket>();
        _logger = Substitute.For<ILogger<IClientObservable>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        var context = Substitute.For<IHttpRequestContext>();
        context.WebSockets.AcceptWebSocket().Returns(_socket);
        context.RequestServices.Returns(_observed.Services);
        var handler = legacy
            ? Substitute.For<IWebSocketConnectionHandler>()
            : Substitute.For<IWebSocketConnectionHandler, IWebSocketMessageSender>();
        handler.HandleIncomingMessages(Arg.Any<IWebSocket>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                call.Arg<CancellationToken>().Register(() => disconnected.TrySetResult());
                return disconnected.Task;
            });
        handler.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RecordSend(call.Arg<CancellationToken>());
                return throws ? throw _error : Task.FromResult(outcome.Error);
            });
        if (handler is IWebSocketMessageSender sender)
        {
            sender.Send(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    RecordSend(call.Arg<CancellationToken>());
                    return Task.FromResult(outcome);
                });
        }
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<object>>(1)));
        var query = new QueryContext(queryName, CorrelationId.New(), Paging.NotPaged, Sorting.None);
        var observable = new ClientEnumerableObservable<string>(query, Items(), interceptors, handler, Substitute.For<IObservableQueryEmissionGuards>(), _logger);
        await observable.HandleConnection(context).WaitAsync(TimeSpan.FromSeconds(5));
    }

    void RecordSend(CancellationToken token)
    {
        _sendToken = token;
        _sends++;
    }

    async IAsyncEnumerable<string> Items([EnumeratorCancellation] CancellationToken token = default)
    {
        try
        {
            foreach (var item in new[] { "first", "second", "third" })
            {
                token.ThrowIfCancellationRequested();
                await Task.Yield();
                _itemsRead++;
                yield return item;
            }
        }
        finally
        {
            _enumeratorDisposed = true;
        }
    }

    void Destroy() => _observed?.Dispose();
}
