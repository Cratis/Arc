// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_an_unrepresentable_subscription_scope : given.a_query_pipeline
{
    readonly FullyQualifiedQueryName _name = "ScopedObservable";
    QueryResult _result;

    void Establish()
    {
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(_name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            var cycle = new CyclicScope();
            cycle.Self = cycle;
            call.Arg<QueryContext>().SubscriptionScope = cycle;
            return Task.FromResult(QueryResult.Success(_correlationId));
        });
    }

    async Task Because() => _result = await _pipeline.Perform(_name, new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_report_the_explicit_scope_failure() =>
        _result.ExceptionMessages.Single().ShouldContain("Subscription scope of type");
    [Fact] void should_not_execute_the_query() =>
        _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
    [Fact] void should_throw_a_custom_exception_for_unrepresentable_scopes()
    {
        var cycle = new CyclicScope();
        cycle.Self = cycle;
        var error = Catch.Exception(() => _ = new ObservableQuerySubscriptionScopeSnapshot(cycle, new ArcOptions().JsonSerializerOptions));
        error.ShouldBeOfExactType<InvalidSubscriptionScope>();
    }

    public class CyclicScope
    {
        public CyclicScope? Self { get; set; }
    }
}
