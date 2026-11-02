// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_anonymous_access_is_already_cached : given.a_guarded_websocket_connection
{
    IServiceScopeFactory _scopeFactory;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _performer.AllowsAnonymousAccess.Returns(true);
        _streamingData = new[] { "snapshot" };
        var original = _guardedServiceProvider;
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(_ => original.CreateScope());
        _guardedServiceProvider = Substitute.For<IServiceProvider>();
        _guardedServiceProvider.GetService(Arg.Any<Type>()).Returns(call =>
            call.Arg<Type>() == typeof(IServiceScopeFactory) ? _scopeFactory : original.GetService(call.Arg<Type>()));
        UseGuards(Substitute.For<IObservableQueryEmissionGuards>());
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact]
    void should_create_a_scope_only_for_the_first_predicate_evaluation_and_one_per_subscription() =>
        _scopeFactory.Received(3).CreateScope(); // 1 predicate scope + 1 subscription scope for each of the 2 subscriptions
    [Fact] void should_deliver_the_first_snapshot() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_deliver_the_second_snapshot() => CountQueryResultsFor(SecondQueryId).ShouldEqual(1);
}
