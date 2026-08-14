// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection.given;

public class a_guarded_client_observable : Specification
{
    protected const string QueryName = "MyApp.Queries.GuardedQuery";

    protected Subject<string> _subject;
    protected IHttpRequestContext _context;
    protected IObservableQueryEmissionGuards _emissionGuards;
    protected ConcurrentQueue<ObservableQueryEmissionContext> _guardCalls;
    protected ConcurrentQueue<SentResult> _sent;
    protected ClaimsPrincipal _principal;
    protected QueryContext _queryContext;
    protected ClientObservable<string> _observable;

    /// <summary>
    /// The verdict for an emission. Assign in a spec's own Establish — it is read when the emission happens.
    /// </summary>
    protected Func<ObservableQueryEmissionContext, ObservableQueryEmissionVerdict> _verdict = _ => ObservableQueryEmissionVerdict.Allow;

    TaskCompletionSource _incomingStarted;
    TaskCompletionSource _incomingCompleted;

    void Establish()
    {
        _subject = new Subject<string>();
        _guardCalls = [];
        _sent = [];
        _incomingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _incomingCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "the-caller")], "test"));
        _queryContext = new QueryContext(QueryName, CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["id"] = 42 });

        var webSocket = Substitute.For<IWebSocket>();
        var webSocketContext = Substitute.For<IWebSocketContext>();
        webSocketContext.AcceptWebSocket().Returns(Task.FromResult(webSocket));

        _context = Substitute.For<IHttpRequestContext>();
        _context.WebSockets.Returns(webSocketContext);
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _context.User.Returns(_principal);

        var webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        webSocketConnectionHandler
            .HandleIncomingMessages(Arg.Any<IWebSocket>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<CancellationToken>().Register(() => _incomingCompleted.TrySetResult());
                _incomingStarted.TrySetResult();
                return _incomingCompleted.Task;
            });
        webSocketConnectionHandler
            .SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var result = callInfo.Arg<QueryResult>();
                _sent.Enqueue(new SentResult(result.IsAuthorized, result.Data));
                return Task.FromResult<Exception?>(null);
            });

        var readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1)));

        _emissionGuards = Substitute.For<IObservableQueryEmissionGuards>();
        _emissionGuards.HasGuards.Returns(true);
        _emissionGuards.Guard(Arg.Any<ObservableQueryEmissionContext>())
            .Returns(callInfo =>
            {
                var emissionContext = callInfo.Arg<ObservableQueryEmissionContext>();
                _guardCalls.Enqueue(emissionContext);
                return Task.FromResult(_verdict(emissionContext));
            });

        var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        hostApplicationLifetime.ApplicationStopping.Returns(CancellationToken.None);

        _observable = new ClientObservable<string>(
            _queryContext,
            _subject,
            readModelInterceptors,
            Substitute.For<IHttpRequestContextAccessor>(),
            webSocketConnectionHandler,
            hostApplicationLifetime,
            _emissionGuards,
            Substitute.For<ILogger<ClientObservable<string>>>());
    }

    protected bool ServerEndedTheConnection => _incomingCompleted.Task.IsCompleted;

    protected async Task RunConnection(Func<Task> script)
    {
        var connectionTask = _observable.HandleConnection(_context);
        await _incomingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await script();

        // Stand in for the client going away, in case the server has not already ended the connection itself.
        _incomingCompleted.TrySetResult();
        await connectionTask.WaitAsync(TimeSpan.FromSeconds(5));
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

    protected record SentResult(bool IsAuthorized, object Data);
}
