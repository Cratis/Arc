// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// A guard's collaborators must come from the subscription's own scope. Resolving them from the root would hand every
/// subscription whichever tenant's — or session's — collaborator happened to be cached there first, and would keep it
/// alive for the lifetime of the process.
/// </summary>
public class and_guard_resolves_scoped_dependencies : given.a_guarded_sse_connection
{
    readonly ConcurrentQueue<ScopedGuardDependency> _resolved = new();

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await WaitFor(() => _resolved.Count == 1);

        _subject.OnNext(["item-b"]);
        await WaitFor(() => _resolved.Count == 2);
    });

    [Fact] void should_resolve_the_dependency_for_every_emission() => _resolved.Count.ShouldEqual(2);
    [Fact] void should_reuse_the_subscription_scope_across_emissions() => _resolved.Distinct().Count().ShouldEqual(1);
    [Fact] void should_not_resolve_from_the_root_provider() => ReferenceEquals(_resolved.First(), _guardedServiceProvider.GetRequiredService<ScopedGuardDependency>()).ShouldBeFalse();
    [Fact] void should_dispose_the_dependency_with_the_subscription() => _resolved.First().IsDisposed.ShouldBeTrue();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(_resolved);
        services.AddScoped<ScopedGuardDependency>();
        guardTypes.Add(typeof(ScopedDependencyGuard));
    }

    public class ScopedGuardDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    public class ScopedDependencyGuard(ConcurrentQueue<ScopedGuardDependency> resolved, ScopedGuardDependency dependency) : IGuardObservableQueryEmission
    {
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            resolved.Enqueue(dependency);
            return Task.FromResult(ObservableQueryEmissionVerdict.Allow);
        }
    }
}
