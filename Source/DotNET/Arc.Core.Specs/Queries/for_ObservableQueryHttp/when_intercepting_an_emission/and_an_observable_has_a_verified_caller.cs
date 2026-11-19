// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_an_observable_has_a_verified_caller : and_a_verified_caller_is_admitted
{
    protected override bool UsesObservable => true;

    [Fact] void should_keep_the_admitted_observable_subject() => _principals.ShouldKeepTheAdmittedSubject();
    [Fact] void should_keep_the_ambient_observable_subject() => _principals.ShouldKeepTheAmbientSubject();
    [Fact] void should_not_grant_host_privileges_to_an_observable() => _principals.ShouldNotGainPrivilege();
    [Fact] void should_not_upgrade_observable_authentication() => _principals.ShouldNotUpgradeAuthentication();
    [Fact] void should_evaluate_both_observable_emissions() => _principals.ShouldEvaluateBothEmissions();
    [Fact] void should_withhold_both_observable_values() => _response.Result.Data.ShouldBeNull();
}
