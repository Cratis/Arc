// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_the_concurrency_wait_is_cancelled : given.a_scoped_websocket_container
{
    const int Blocking = 8;
    int _created;
    int _disposed;

    void Establish()
    {
        // Eight subscriptions fill the connection's concurrency limit and block in the query; the ninth waits for a turn.
        _queryIds = [.. Enumerable.Range(1, Blocking + 1).Select(_ => $"query-{_}")];
        _performResult = async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return QueryResult.Success(CorrelationId.New());
        };

        var original = _guardedServiceProvider;
        var factory = Substitute.For<IServiceScopeFactory>();
        factory.CreateScope().Returns(_ =>
        {
            Interlocked.Increment(ref _created);
            var inner = original.CreateScope();
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(inner.ServiceProvider);
            scope.When(s => s.Dispose()).Do(_ =>
            {
                Interlocked.Increment(ref _disposed);
                inner.Dispose();
            });
            return scope;
        });
        var counting = Substitute.For<IServiceProvider>();
        counting.GetService(Arg.Any<Type>()).Returns(call =>
            call.Arg<Type>() == typeof(IServiceScopeFactory) ? factory : original.GetService(call.Arg<Type>()));
        _guardedServiceProvider = counting;
        UseGuards(Substitute.For<IObservableQueryEmissionGuards>());
    }

    async Task Because() => await RunConnection(() => WaitFor(() => _resolvedDuringPerform.Count == Blocking));

    [Fact] void should_only_run_the_query_for_the_subscriptions_that_got_a_turn() => _resolvedDuringPerform.Count.ShouldEqual(Blocking);
    [Fact] void should_dispose_every_probe_exactly_once() => _resolvedDuringPerform.All(_ => _.Disposals == 1).ShouldBeTrue();
    [Fact] void should_create_a_scope_for_every_subscription() => _created.ShouldBeGreaterThanOrEqual(Blocking + 1);
    [Fact] void should_dispose_every_scope_it_created() => _disposed.ShouldEqual(_created);
}
