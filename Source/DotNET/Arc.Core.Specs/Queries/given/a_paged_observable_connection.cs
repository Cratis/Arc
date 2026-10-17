// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.given;

public class a_paged_observable_connection : Specification
{
    protected static readonly PagingInfo ExpectedPaging = new(2, 10, 73);

    protected static object? EmittedValue(string shape) => shape switch
    {
        "collection" => (int[])[1, 2],
        "string" => "result",
        "value" => 42,
        _ => null
    };

    protected static async Task<PagingInfo> CapturePaging(object? value, bool useSse)
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        using var subject = new BehaviorSubject<object?>(value);
        var queryContext = new QueryContext("TestQuery", CorrelationId.New(), new Paging(2, 10, true), Sorting.None)
        {
            TotalItems = 73
        };
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(services);
        context.RequestAborted.Returns(CancellationToken.None);
        context.User.Returns(new ClaimsPrincipal());
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<object>>(1)));
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        var sent = new TaskCompletionSource<PagingInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        IClientObservable observable;
        if (useSse)
        {
            var options = Options.Create(new ArcOptions());
            context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var result = JsonSerializer.Deserialize<QueryResult>(call.Arg<string>()["data: ".Length..].Trim(), options.Value.JsonSerializerOptions)!;
                sent.TrySetResult(result.Paging);
                return Task.CompletedTask;
            });
            observable = new ClientObservableSSE<object?>(queryContext, subject, interceptors, accessor, options, lifetime, guards, Substitute.For<ILogger<ClientObservableSSE<object?>>>());
        }
        else
        {
            var socket = Substitute.For<IWebSocket>();
            var sockets = Substitute.For<IWebSocketContext>();
            sockets.AcceptWebSocket().Returns(Task.FromResult(socket));
            context.WebSockets.Returns(sockets);
            var incoming = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = Substitute.For<IWebSocketConnectionHandler>();
            handler.HandleIncomingMessages(Arg.Any<IWebSocket>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    await using var registration = call.Arg<CancellationToken>().Register(() => incoming.TrySetResult());
                    await incoming.Task;
                });
            handler.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                sent.TrySetResult(call.Arg<QueryResult>().Paging);
                return Task.FromResult<Exception?>(null);
            });
            observable = new ClientObservable<object?>(queryContext, subject, interceptors, accessor, handler, lifetime, guards, Substitute.For<ILogger<ClientObservable<object?>>>());
        }

        var connection = observable.HandleConnection(context);
        try
        {
            return await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            subject.OnCompleted();
            await connection.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
