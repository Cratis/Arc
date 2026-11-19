// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.given;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_a_verified_caller_is_admitted : given.a_guarded_client_observable_sse
{
    protected emission_principals _principals;
    protected virtual bool Anonymous => false;

    void Establish()
    {
        _principals = new(Anonymous);
        _principals.Attach(_context);
        _verdict = _principals.Evaluate;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _principals.ChangeRequestPrincipal();
        _subject.OnNext("first");
        await WaitFor(() => _guardCalls.Count == 1);
        _subject.OnNext("second");
        await WaitFor(() => _guardCalls.Count == 2);
    });

    [Fact] void should_keep_the_admitted_subject() => _principals.ShouldKeepTheAdmittedSubject();
    [Fact] void should_keep_the_ambient_subject() => _principals.ShouldKeepTheAmbientSubject();
    [Fact] void should_not_gain_host_privileges() => _principals.ShouldNotGainPrivilege();
    [Fact] void should_not_upgrade_authentication() => _principals.ShouldNotUpgradeAuthentication();
    [Fact] void should_evaluate_both_emissions() => _principals.ShouldEvaluateBothEmissions();
    [Fact] void should_withhold_both_values() => WrittenResults.ShouldBeEmpty();
}
