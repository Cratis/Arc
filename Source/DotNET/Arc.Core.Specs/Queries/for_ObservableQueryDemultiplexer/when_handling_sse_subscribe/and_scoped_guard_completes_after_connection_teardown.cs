// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_scoped_guard_completes_after_connection_teardown : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource _guardRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _guardStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<ScopedDependency> _dependencies = [];
    bool _scopeWasAliveWhileGuardWasBlocked;

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await _guardStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await _connectionCancellation.CancelAsync();
        _scopeWasAliveWhileGuardWasBlocked = _dependencies.Count == 1 && !_dependencies.Single().IsDisposed;

        _guardRelease.TrySetResult();
        await WaitFor(() => _dependencies.Single().IsDisposed);
    });

    [Fact] void should_keep_the_subscription_scope_alive_while_the_guard_exits() => _scopeWasAliveWhileGuardWasBlocked.ShouldBeTrue();
    [Fact] void should_dispose_the_subscription_scope_after_the_guard_exits() => _dependencies.Single().IsDisposed.ShouldBeTrue();
    [Fact] void should_not_send_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_unauthorized_after_disconnect() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_a_late_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(new GuardState(_guardStarted, _guardRelease, _dependencies));
        services.AddScoped<ScopedDependency>();
        guardTypes.Add(typeof(BlockedGuard));
    }

    public sealed record GuardState(
        TaskCompletionSource Started,
        TaskCompletionSource Release,
        List<ScopedDependency> Dependencies);

    public class ScopedDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class BlockedGuard(GuardState state, ScopedDependency dependency) : IGuardObservableQueryEmission
    {
        public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            state.Dependencies.Add(dependency);
            state.Started.TrySetResult();
            await state.Release.Task;
            return ObservableQueryEmissionVerdict.Allow;
        }
    }
}
