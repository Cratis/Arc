// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.ArcCore.Specs.for_PublishedArcCore;

public class when_using_a_caller_claim_lookup_without_the_claim : given.a_caller_lookup_request
{
    async Task Because() => await PostLookup("by-claim", CallerWithoutLogin);

    [PublishedArcCoreFact] void should_report_a_configuration_failure() => AssertConfigurationFailure();
    [PublishedArcCoreFact] async Task should_refuse_before_resolving_the_missing_claim() => await AssertUnsupportedLookup("Samples.Features.CallerKeyedLookups.LoginClaim", "ByClaim.RefuseCallerClaimLookup");
    [PublishedArcCoreFact] void should_not_execute_using_the_service_provider_fallback() => _body.GetProperty("isSuccess").GetBoolean().ShouldBeFalse();
    [PublishedArcCoreFact] void should_not_misrepresent_the_refusal_as_unauthorized() => _body.GetProperty("isAuthorized").GetBoolean().ShouldBeTrue();
}
