// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

/// <summary>
/// A scoped interceptor can still be awaiting when the subscription is removed. Teardown must cancel and detach the
/// observer immediately, but keep the scope alive until that callback exits.
/// </summary>
public class and_scoped_interceptor_completes_after_unsubscribe : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource _interceptorRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _interceptorStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<ScopedDependency> _dependencies = [];
    bool _scopeWasAliveWhileInterceptorWasBlocked;

    void Establish()
    {
        _readModelInterceptors.Intercept(
                Arg.Any<Type>(),
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IServiceProvider>())
            .Returns(callInfo => Intercept(
                callInfo.ArgAt<IEnumerable<object>>(1),
                callInfo.ArgAt<IServiceProvider>(2)));
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await _interceptorStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Unsubscribe(FirstQueryId);
        _scopeWasAliveWhileInterceptorWasBlocked = _dependencies.Count == 1 && !_dependencies.Single().IsDisposed;

        _interceptorRelease.TrySetResult();
        await WaitFor(() => _dependencies.Single().IsDisposed);
    });

    [Fact] void should_keep_the_subscription_scope_alive_while_the_interceptor_exits() => _scopeWasAliveWhileInterceptorWasBlocked.ShouldBeTrue();
    [Fact] void should_dispose_the_subscription_scope_after_the_interceptor_exits() => _dependencies.Single().IsDisposed.ShouldBeTrue();
    [Fact] void should_not_send_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_a_late_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes) =>
        services.AddScoped<ScopedDependency>();

    async Task<IEnumerable<object>> Intercept(IEnumerable<object> data, IServiceProvider provider)
    {
        _dependencies.Add(provider.GetRequiredService<ScopedDependency>());
        _interceptorStarted.TrySetResult();
        await _interceptorRelease.Task;
        return data;
    }

    public class ScopedDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
