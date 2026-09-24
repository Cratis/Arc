// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_cancellation_callback_fails_during_an_emission
{
    [Fact]
    public async Task should_keep_request_resources_alive_until_the_guard_exits_and_propagate_the_failure()
    {
        await using var services = new ServiceCollection().AddScoped<RequestResource>().BuildServiceProvider();
        var scope = services.CreateAsyncScope();
        var resource = scope.ServiceProvider.GetRequiredService<RequestResource>();
        var subject = new Subject<int>();
        var context = Substitute.For<IHttpRequestContext>();
        var webSocket = Substitute.For<IWebSocket>();
        var webSockets = Substitute.For<IWebSocketContext>();
        webSockets.AcceptWebSocket().Returns(async _ =>
        {
            await Task.Yield();
            return webSocket;
        });
        context.WebSockets.Returns(webSockets);
        context.RequestServices.Returns(scope.ServiceProvider);
        context.RequestAborted.Returns(CancellationToken.None);
        var incoming = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = Substitute.For<IWebSocketConnectionHandler>();
        handler.HandleIncomingMessages(Arg.Any<IWebSocket>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            receiving.TrySetResult();
            await incoming.Task;
        });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var guardExited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("cancellation callback failed");
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async call =>
        {
            await using var registration = call.Arg<ObservableQueryEmissionContext>().CancellationToken.Register(() =>
            {
                cancellationRan.TrySetResult();
                throw failure;
            });
            entered.TrySetResult();
            await release.Task;
            scope.ServiceProvider.GetRequiredService<RequestResource>().Disposed.ShouldBeFalse();
            guardExited.TrySetResult();
            return ObservableQueryEmissionVerdict.Allow;
        });
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<object>>(1)));
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        var observable = new ClientObservable<int>(
            new QueryContext("Controller.Watch", CorrelationId.New(), Paging.NotPaged, Sorting.None),
            subject,
            interceptors,
            Substitute.For<IHttpRequestContextAccessor>(),
            handler,
            lifetime,
            guards,
            Substitute.For<ILogger<ClientObservable<int>>>());

        var connection = observable.HandleConnection(context);
        var requestFinished = EndRequest();
        async Task EndRequest()
        {
            try
            {
                await connection;
            }
            finally
            {
                await scope.DisposeAsync();
            }
        }
        try
        {
            await receiving.Task.WaitAsync(TimeSpan.FromSeconds(5));
            subject.OnNext(1);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            incoming.TrySetResult();
            await cancellationRan.Task.WaitAsync(TimeSpan.FromSeconds(5));
            connection.IsCompleted.ShouldBeFalse();
            resource.Disposed.ShouldBeFalse();
            release.TrySetResult();
            await guardExited.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var thrown = await Assert.ThrowsAsync<AggregateException>(() => requestFinished.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Same(failure, Assert.Single(thrown.InnerExceptions));
            resource.Disposed.ShouldBeTrue();
        }
        finally
        {
            release.TrySetResult();
            incoming.TrySetResult();
            try
            {
                await requestFinished.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Asserted above; ensure a failed connection has still finished disposing its request scope.
            }
        }
    }

    sealed class RequestResource : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
