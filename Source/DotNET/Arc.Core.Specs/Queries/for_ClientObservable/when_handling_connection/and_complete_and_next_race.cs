// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_complete_and_next_race
{
    [Fact]
    public async Task should_not_throw_when_complete_is_called_multiple_times()
    {
        var subject = new Subject<int>();

        var webSocket = Substitute.For<IWebSocket>();
        var webSocketContext = Substitute.For<IWebSocketContext>();
        webSocketContext.AcceptWebSocket().Returns(Task.FromResult(webSocket));

        var httpContext = Substitute.For<IHttpRequestContext>();
        httpContext.WebSockets.Returns(webSocketContext);

        var hostLifetime = Substitute.For<IHostApplicationLifetime>();
        hostLifetime.ApplicationStopping.Returns(CancellationToken.None);

        var logger = Substitute.For<ILogger<ClientObservable<int>>>();

        // HandleIncomingMessages blocks until the CancellationToken is cancelled
        var incomingMessagesTcs = new TaskCompletionSource();
        var handleIncomingStarted = new TaskCompletionSource();
        var webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        webSocketConnectionHandler
            .HandleIncomingMessages(webSocket, Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var token = ci.Arg<CancellationToken>();
                token.Register(() => incomingMessagesTcs.TrySetResult());
                handleIncomingStarted.TrySetResult();
                return incomingMessagesTcs.Task;
            });

        webSocketConnectionHandler
            .SendMessage(webSocket, Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Exception?>(null));

        var queryContext = new QueryContext(
            new FullyQualifiedQueryName("[TestApp].[TestQuery]"),
            CorrelationId.New(),
            new Paging(0, 10, true),
            Sorting.None);

        var observable = new ClientObservable<int>(
            queryContext,
            subject,
            webSocketConnectionHandler,
            hostLifetime,
            logger);

        // Act — run connection in background, then trigger the races
        var connectionTask = Task.Run(() => observable.HandleConnection(httpContext));

        // Wait until HandleIncomingMessages is actually being awaited
        await handleIncomingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Emit items and concurrently complete
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnCompleted();
        subject.OnCompleted(); // Double-complete triggered the TrySetResult bug

        // Connection should terminate cleanly
        var timeout = Task.Delay(2000);
        var completed = await Task.WhenAny(connectionTask, timeout);

        if (completed == timeout)
        {
            throw new TimeoutException("Connection did not complete in time after subject completed");
        }

        // Should not throw (was throwing InvalidOperationException from tcs.SetResult on double-call)
        await connectionTask;
    }

    [Fact]
    public async Task should_not_throw_when_next_is_called_after_complete()
    {
        var subject = new Subject<int>();

        var webSocket = Substitute.For<IWebSocket>();
        var webSocketContext = Substitute.For<IWebSocketContext>();
        webSocketContext.AcceptWebSocket().Returns(Task.FromResult(webSocket));

        var httpContext = Substitute.For<IHttpRequestContext>();
        httpContext.WebSockets.Returns(webSocketContext);

        var hostLifetime = Substitute.For<IHostApplicationLifetime>();
        hostLifetime.ApplicationStopping.Returns(CancellationToken.None);

        var logger = Substitute.For<ILogger<ClientObservable<int>>>();

        var handleStarted2 = new TaskCompletionSource();
        var webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        webSocketConnectionHandler
            .HandleIncomingMessages(webSocket, Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var token = ci.Arg<CancellationToken>();
                var tcs = new TaskCompletionSource();
                token.Register(() => tcs.TrySetResult());
                handleStarted2.TrySetResult();
                return tcs.Task;
            });

        webSocketConnectionHandler
            .SendMessage(webSocket, Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Exception?>(null));

        var queryContext = new QueryContext(
            new FullyQualifiedQueryName("[TestApp].[TestQuery]"),
            CorrelationId.New(),
            new Paging(0, 10, true),
            Sorting.None);

        var observable = new ClientObservable<int>(
            queryContext,
            subject,
            webSocketConnectionHandler,
            hostLifetime,
            logger);

        var connectionTask = Task.Run(() => observable.HandleConnection(httpContext));

        await handleStarted2.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Complete first, then emit — Next() should guard against CancellationToken
        subject.OnCompleted();
        subject.OnNext(42); // Must not cause exception or deadlock

        var timeout = Task.Delay(2000);
        var completed = await Task.WhenAny(connectionTask, timeout);

        if (completed == timeout)
        {
            throw new TimeoutException("Connection did not complete in time");
        }

        await connectionTask;
    }
}
