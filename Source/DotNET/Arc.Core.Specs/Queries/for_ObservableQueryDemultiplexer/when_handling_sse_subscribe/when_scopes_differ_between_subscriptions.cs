// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class when_scopes_differ_between_subscriptions : given.a_guarded_sse_connection
{
    readonly ConcurrentQueue<string> _seenScopes = [];
    int _admitted;
    string _currentOrganization;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _currentOrganization = "former";
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var scope = Interlocked.Increment(ref _admitted) == 1 ? "former" : "new";
                var context = new QueryContext(QueryName, CorrelationId.New(), Paging.NotPaged, Sorting.None, CoercedArguments)
                {
                    SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(
                        new List<string> { scope }, _arcOptions.Value.JsonSerializerOptions)
                };
                var result = QueryResult.Success(context.CorrelationId);
                result.AuthorizedArguments = context.Arguments;
                result.AuthorizedQueryContext = context;
                result.Data = _subject;
                return Task.FromResult(result);
            });
        _verdict = context =>
        {
            var captured = (List<string>)context.SubscriptionScope!;
            _seenScopes.Enqueue(captured[0]);
            var allowed = captured[0] == "new" || captured[0] == _currentOrganization;
            captured[0] = "mutated-by-guard";
            return allowed ? ObservableQueryEmissionVerdict.Allow : ObservableQueryEmissionVerdict.DenyAndTerminate;
        };
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["initial"]);
        await WaitFor(() => _guardCalls.Count >= 2);
        _currentOrganization = "new";
        _subject.OnNext(["membership-changed"]);
        await WaitFor(() => HasUnauthorizedFor(FirstQueryId) && CountQueryResultsFor(SecondQueryId) >= 1);
        _subject.OnNext(["still-subscribed"]);
        await WaitFor(() => CountQueryResultsFor(SecondQueryId) >= 2);
    });

    [Fact] void should_terminate_the_old_scope_only() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_keep_the_other_subscription_running() => (CountQueryResultsFor(SecondQueryId) >= 2).ShouldBeTrue();
    [Fact] void should_not_share_mutations_between_emissions_or_subscriptions() =>
        _seenScopes.All(scope => scope == "former" || scope == "new").ShouldBeTrue();
    [Fact] void should_retain_both_captured_scopes() =>
        _seenScopes.Distinct().Order().ShouldEqual(["former", "new"]);
    [Fact] void should_keep_arguments_unchanged() =>
        _guardCalls.All(call => (int)call.Arguments["id"] == 42).ShouldBeTrue();
}
