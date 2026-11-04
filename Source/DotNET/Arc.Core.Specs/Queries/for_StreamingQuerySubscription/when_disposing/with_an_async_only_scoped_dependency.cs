// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_disposing;

public class with_an_async_only_scoped_dependency : Specification
{
    ServiceProvider _provider;
    StreamingQuerySubscription _subscription;
    Probe _probe;
    Exception _error;

    void Establish()
    {
        _provider = new ServiceCollection().AddScoped<Probe>().BuildServiceProvider();
        var scope = _provider.CreateScope();
        _probe = scope.ServiceProvider.GetRequiredService<Probe>();
        _subscription = new(CancellationToken.None);
        _subscription.AddResource(scope);
    }

    void Because() => _error = Catch.Exception(_subscription.Dispose);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_dispose_the_probe_exactly_once() => _probe.Disposals.ShouldEqual(1);

    async Task Destroy() => await _provider.DisposeAsync();

    public sealed class Probe : IAsyncDisposable
    {
        public int Disposals { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
