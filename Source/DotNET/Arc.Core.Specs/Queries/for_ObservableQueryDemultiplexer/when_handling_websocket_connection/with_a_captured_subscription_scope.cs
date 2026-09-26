// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class with_a_captured_subscription_scope : given.a_guarded_websocket_connection
{
    readonly ConcurrentQueue<string> _seenScopes = [];

    void Establish()
    {
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var context = new QueryContext(QueryName, CorrelationId.New(), Paging.NotPaged, Sorting.None, CoercedArguments)
                {
                    SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(
                        new List<string> { "former" }, _arcOptions.Value.JsonSerializerOptions)
                };
                var result = QueryResult.Success(context.CorrelationId);
                result.AuthorizedArguments = context.Arguments;
                result.AuthorizedQueryContext = context;
                result.Data = _subject;
                return Task.FromResult(result);
            });
        _verdict = context =>
        {
            var scope = (List<string>)context.SubscriptionScope!;
            _seenScopes.Enqueue(scope[0]);
            scope[0] = "mutated";
            return ObservableQueryEmissionVerdict.Allow;
        };
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["first"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) >= 1);
        _subject.OnNext(["second"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) >= 2);
    });

    [Fact] void should_restore_the_captured_scope_on_each_emission() => _seenScopes.ShouldEqual(["former", "former"]);
    [Fact] void should_keep_the_coerced_arguments() => _guardCalls.All(call => (int)call.Arguments["id"] == 42).ShouldBeTrue();
}
