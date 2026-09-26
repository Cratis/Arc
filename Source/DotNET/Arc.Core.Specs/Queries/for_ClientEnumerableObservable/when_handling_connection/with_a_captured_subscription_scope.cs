// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class with_a_captured_subscription_scope : given.a_guarded_client_enumerable_observable
{
    readonly List<string> _seenScopes = [];

    void Establish()
    {
        _queryContext.SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(
            new List<string> { "former" }, new ArcOptions().JsonSerializerOptions);
        _verdict = context =>
        {
            var scope = (List<string>)context.SubscriptionScope!;
            _seenScopes.Add(scope[0]);
            scope[0] = "mutated";
            return ObservableQueryEmissionVerdict.Allow;
        };
    }

    async Task Because() => await RunConnection();

    [Fact] void should_pass_the_original_scope_for_each_item() => _seenScopes.ShouldEqual(["former", "former"]);
    [Fact] void should_keep_the_captured_arguments() => _guardCalls.All(call => (int)call.Arguments["id"] == 42).ShouldBeTrue();
}
