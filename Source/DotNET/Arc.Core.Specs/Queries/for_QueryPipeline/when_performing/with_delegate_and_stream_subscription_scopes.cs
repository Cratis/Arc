// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_delegate_and_stream_subscription_scopes : given.a_query_pipeline
{
    readonly FullyQualifiedQueryName _name = "ScopedObservable";
    object _scope;
    QueryResult _delegateResult;
    QueryResult _streamResult;

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
            call.Arg<QueryContext>().SubscriptionScope = _scope;
            return Task.FromResult(QueryResult.Success(_correlationId));
        });
    }

    async Task Because()
    {
        _scope = new Func<int>(() => 42);
        _delegateResult = await _pipeline.Perform(_name, new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);
        _scope = new MemoryStream([1, 2]);
        _streamResult = await _pipeline.Perform(_name, new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);
    }

    [Fact] void should_reject_the_delegate() => _delegateResult.ExceptionMessages.Single().ShouldContain("Subscription scope of type");
    [Fact] void should_reject_the_stream() => _streamResult.ExceptionMessages.Single().ShouldContain("Subscription scope of type");
    [Fact] void should_not_execute_either_query() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
}
