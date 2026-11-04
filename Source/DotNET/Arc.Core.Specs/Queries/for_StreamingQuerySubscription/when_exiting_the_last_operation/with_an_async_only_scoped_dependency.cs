// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_exiting_the_last_operation;

public class with_an_async_only_scoped_dependency : Specification
{
    ServiceProvider _provider;
    StreamingQuerySubscription _subscription;
    when_disposing.with_an_async_only_scoped_dependency.Probe _probe;
    int _disposalsBeforeExit;
    Exception _error;

    void Establish()
    {
        _provider = new ServiceCollection().AddScoped<when_disposing.with_an_async_only_scoped_dependency.Probe>().BuildServiceProvider();
        var scope = _provider.CreateScope();
        _probe = scope.ServiceProvider.GetRequiredService<when_disposing.with_an_async_only_scoped_dependency.Probe>();
        _subscription = new(CancellationToken.None);
        _subscription.AddResource(scope);
        _subscription.TryEnter().ShouldBeTrue();
    }

    void Because()
    {
        _subscription.Dispose();
        _disposalsBeforeExit = _probe.Disposals;
        _error = Catch.Exception(_subscription.Exit);
    }

    [Fact] void should_keep_the_probe_until_exit() => _disposalsBeforeExit.ShouldEqual(0);
    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_dispose_the_probe_exactly_once() => _probe.Disposals.ShouldEqual(1);

    async Task Destroy() => await _provider.DisposeAsync();
}
