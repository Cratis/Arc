// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class when_scope_membership_changes : given.a_guarded_client_observable
{
    string _currentOrganization;

    void Establish()
    {
        _currentOrganization = "former";
        _queryContext.SubscriptionScopeSnapshot = new ObservableQuerySubscriptionScopeSnapshot(
            new List<string> { "former" }, new ArcOptions().JsonSerializerOptions);
        _verdict = context => ((List<string>)context.SubscriptionScope!)[0] == _currentOrganization
            ? ObservableQueryEmissionVerdict.Allow
            : ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("initial");
        await WaitFor(() => _guardCalls.Count == 1);
        _currentOrganization = "new";
        _subject.OnNext("after-membership-change");
        await WaitFor(() => _guardCalls.Count == 2 && ServerEndedTheConnection);
    });

    [Fact] void should_keep_the_original_scope_on_both_emissions() =>
        _guardCalls.All(call => ((List<string>)call.SubscriptionScope!)[0] == "former").ShouldBeTrue();
    [Fact] void should_terminate_after_membership_changes_without_changing_arguments() =>
        _sent.Count(result => !result.IsAuthorized).ShouldEqual(1);
    [Fact] void should_keep_the_arguments_unchanged() =>
        _guardCalls.All(call => (int)call.Arguments["id"] == 42).ShouldBeTrue();
}
