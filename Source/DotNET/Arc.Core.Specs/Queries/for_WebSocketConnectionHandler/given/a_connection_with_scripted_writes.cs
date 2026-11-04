// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Arc.Observability;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_WebSocketConnectionHandler.given;

public class a_connection_with_scripted_writes : Specification
{
    protected ConcurrentQueue<bool> _firstEmissions;
    internal ObservedQueryServices _observed;

    protected async Task Run(bool enumerable, bool open, Func<Task> send, bool legacy = false, bool initiallySent = false)
    {
        const string queryName = "ScriptedWrites";
        _firstEmissions = [];
        _observed = new ObservedQueryServices([queryName]);
        var incomingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var incoming = new TaskCompletionSource<WebSocketReceiveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var webSocket = Substitute.For<IWebSocket>();
        webSocket.State.Returns(open ? WebSocketState.Open : WebSocketState.Closed);
        var writes = 0;
        webSocket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => initiallySent && ++writes == 1 ? Task.CompletedTask : send());
        webSocket.Receive(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            call.Arg<CancellationToken>().Register(() => incoming.TrySetCanceled());
            incomingStarted.TrySetResult();
            return incoming.Task;
        });
        var context = Substitute.For<IHttpRequestContext>();
        var sockets = Substitute.For<IWebSocketContext>();
        sockets.AcceptWebSocket().Returns(webSocket);
        context.WebSockets.Returns(sockets);
        context.RequestServices.Returns(_observed.Services);
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<object>>(1)));
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(call =>
        {
            _firstEmissions.Enqueue(call.Arg<ObservableQueryEmissionContext>().IsFirstEmission);
            return Task.FromResult(_firstEmissions.Count == (initiallySent ? 4 : 3) ? ObservableQueryEmissionVerdict.DenyAndTerminate : ObservableQueryEmissionVerdict.Allow);
        });
        var builtIn = new WebSocketConnectionHandler(Options.Create(new ArcOptions()), Substitute.For<ILogger<WebSocketConnectionHandler>>());
        IWebSocketConnectionHandler handler = builtIn;
        if (legacy)
        {
            handler = Substitute.For<IWebSocketConnectionHandler>();
            handler.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns((Exception?)null);
            handler.HandleIncomingMessages(Arg.Any<IWebSocket>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
                .Returns(call => builtIn.HandleIncomingMessages(call.Arg<IWebSocket>(), call.Arg<SemaphoreSlim>(), call.Arg<CancellationToken>()));
        }
        var query = new QueryContext(queryName, CorrelationId.New(), Paging.NotPaged, Sorting.None, QueryArguments.Empty);
        if (enumerable)
        {
            var observable = new ClientEnumerableObservable<string>(query, Items(initiallySent), interceptors, handler, guards, Substitute.For<ILogger<IClientObservable>>());
            await observable.HandleConnection(context).WaitAsync(TimeSpan.FromSeconds(5));
        }
        else
        {
            using var subject = new Subject<string>();
            var lifetime = Substitute.For<IHostApplicationLifetime>();
            var observable = new ClientObservable<string>(query, subject, interceptors, Substitute.For<IHttpRequestContextAccessor>(), handler, lifetime, guards, Substitute.For<ILogger<ClientObservable<string>>>());
            var connection = observable.HandleConnection(context);
            await incomingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            subject.OnNext("one");
            subject.OnNext("two");
            if (initiallySent)
            {
                subject.OnNext("three");
            }
            subject.OnNext("end");
            await connection.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    static async IAsyncEnumerable<string> Items(bool initiallySent)
    {
        await Task.CompletedTask;
        yield return "one";
        yield return "two";
        if (initiallySent)
        {
            yield return "three";
        }
        yield return "end";
    }

    void Destroy() => _observed?.Dispose();
}
