// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class when_scope_membership_changes : given.a_guarded_client_observable_sse
{
    string _currentOrganization;

    void Establish()
    {
        _currentOrganization = "former";
        _queryContext.SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(
            new List<string> { "former" }, _arcOptions.Value.JsonSerializerOptions);
        _verdict = context => ((List<string>)context.SubscriptionScope!)[0] == _currentOrganization
            ? ObservableQueryEmissionVerdict.Allow
            : ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("initial");
        await WaitFor(() => WrittenResults.Count() == 1);
        _currentOrganization = "new";
        _subject.OnNext("after-membership-change");
        await WaitFor(() => _guardCalls.Count == 2 && WrittenResults.Count() == 2);
    });

    [Fact] void should_preserve_the_old_scope_and_arguments() =>
        _guardCalls.All(call => ((List<string>)call.SubscriptionScope!)[0] == "former" && (int)call.Arguments["id"] == 42).ShouldBeTrue();
    [Fact] void should_send_the_terminal_denial() => WrittenResults.Last().IsAuthorized.ShouldBeFalse();
}
