// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_guarding_websocket_emissions;

public class and_an_anonymous_caller_is_admitted : and_a_verified_caller_is_admitted
{
    protected override bool Anonymous => true;

    [Fact] void should_keep_the_anonymous_subject() => _principals.ShouldKeepTheAdmittedSubject();
    [Fact] void should_keep_anonymous_ambient_identity() => _principals.ShouldKeepTheAmbientSubject();
    [Fact] void should_not_grant_host_privileges_to_an_anonymous_caller() => _principals.ShouldNotGainPrivilege();
    [Fact] void should_keep_the_caller_unauthenticated() => _principals.ShouldNotUpgradeAuthentication();
    [Fact] void should_evaluate_both_anonymous_emissions() => _principals.ShouldEvaluateBothEmissions();
    [Fact] void should_withhold_both_values_from_an_anonymous_caller() => CountQueryResultsFor("the-query").ShouldEqual(0);
}
