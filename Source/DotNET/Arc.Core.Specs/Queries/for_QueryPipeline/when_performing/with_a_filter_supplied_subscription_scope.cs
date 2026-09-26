// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_filter_supplied_subscription_scope : given.a_query_pipeline
{
    readonly FullyQualifiedQueryName _name = "ScopedObservable";
    readonly List<string> _membership = ["former-organization"];
    QueryResult _result;
    string _scopeSeenByPerformer;

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
            call.Arg<QueryContext>().SubscriptionScope = _membership;
            return Task.FromResult(QueryResult.Success(_correlationId));
        });
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(call =>
        {
            var context = call.Arg<QueryContext>();
            _scopeSeenByPerformer = ((List<string>)context.SubscriptionScope!)[0];
            _membership[0] = "new-organization";
            return ValueTask.FromResult<object?>(null);
        });
    }

    async Task Because() => _result = await _pipeline.Perform(_name, new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_capture_the_filter_scope_before_execution() =>
        ((List<string>)_result.AuthorizedQueryContext!.CreateSubscriptionScope()!)[0].ShouldEqual("former-organization");
    [Fact] void should_not_change_the_scope_seen_by_the_performer() => _scopeSeenByPerformer.ShouldEqual("former-organization");
    [Fact] void should_give_each_emission_an_independent_copy()
    {
        var first = (List<string>)_result.AuthorizedQueryContext!.CreateSubscriptionScope()!;
        first[0] = "tampered";
        ((List<string>)_result.AuthorizedQueryContext.CreateSubscriptionScope()!)[0].ShouldEqual("former-organization");
    }
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
