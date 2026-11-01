// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionServiceScope.when_created_from_a_root_provider;

public class with_a_scoped_probe : Specification
{
    ServiceProvider _root;
    ObservableQuerySubscriptionServiceScope _holder;
    Probe _rootProbe;
    Probe _scopedProbe;
    IServiceProvider _provider;
    Exception _afterDisposal;

    void Establish()
    {
        _root = new ServiceCollection().AddScoped<Probe>().BuildServiceProvider();
        _rootProbe = _root.GetRequiredService<Probe>();
        _holder = new ObservableQuerySubscriptionServiceScope(_root);
        _provider = _holder.ServiceProvider;
        _scopedProbe = _provider.GetRequiredService<Probe>();
    }

    void Because()
    {
        _holder.Dispose();
        _afterDisposal = Catch.Exception(() => _provider.GetService(typeof(IServiceScopeFactory)));
    }

    [Fact] void should_not_be_the_root_provider() => _provider.ShouldNotBeSame(_root);
    [Fact] void should_resolve_its_own_instance() => _scopedProbe.ShouldNotBeSame(_rootProbe);
    [Fact] void should_dispose_the_scoped_instance() => _scopedProbe.Disposals.ShouldEqual(1);
    [Fact] void should_not_dispose_the_root_instance() => _rootProbe.Disposals.ShouldEqual(0);
    [Fact] void should_fail_closed_after_disposal() => _afterDisposal.ShouldBeOfExactType<ObjectDisposedException>();

    public class Probe : IDisposable
    {
        public int Disposals { get; private set; }

        public void Dispose() => Disposals++;
    }
}
