// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_streaming;

public class and_unsubscription_fails_during_an_emission
{
    [Fact]
    public async Task should_drain_before_propagating_the_disposal_failure()
    {
        await using var services = new ServiceCollection().AddScoped<RequestResource>().BuildServiceProvider();
        var scope = services.CreateAsyncScope();
        var resource = scope.ServiceProvider.GetRequiredService<RequestResource>();
        var subject = Substitute.For<ISubject<int>>();
        IObserver<int>? observer = null;
        var failure = new InvalidOperationException("subscription disposal failed");
        var subscription = Substitute.For<IDisposable>();
        subscription.When(_ => _.Dispose()).Do(_ => throw failure);
        subject.Subscribe(Arg.Any<IObserver<int>>()).Returns(call =>
        {
            observer = call.Arg<IObserver<int>>();
            return subscription;
        });
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(scope.ServiceProvider);
        context.RequestAborted.Returns(CancellationToken.None);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var guardExited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async _ =>
        {
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
        var observable = new ClientObservableSSE<int>(
            new QueryContext("Controller.Watch", CorrelationId.New(), Paging.NotPaged, Sorting.None),
            subject,
            interceptors,
            Substitute.For<IHttpRequestContextAccessor>(),
            Options.Create(new ArcOptions()),
            lifetime,
            guards,
            Substitute.For<ILogger<ClientObservableSSE<int>>>());

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
            observer.ShouldNotBeNull();
            observer.OnNext(1);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            observer.OnCompleted();
            connection.IsCompleted.ShouldBeFalse();
            resource.Disposed.ShouldBeFalse();
            release.TrySetResult();
            await guardExited.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => requestFinished.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Same(failure, thrown);
            resource.Disposed.ShouldBeTrue();
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await requestFinished.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (InvalidOperationException)
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
