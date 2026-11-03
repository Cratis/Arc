// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.ArcCore.Specs.for_PublishedArcCore;

public class when_using_a_caller_subject_lookup : given.a_caller_lookup_request
{
    async Task Because() => await PostLookup("by-subject", CallerWithLogin);

    [PublishedArcCoreFact] void should_report_a_configuration_failure() => AssertConfigurationFailure();
    [PublishedArcCoreFact] async Task should_discover_and_refuse_the_generic_subject_lookup() => await AssertUnsupportedLookup("Cratis.Arc.Commands.CallerSubject", "BySubject.RefuseCallerSubjectLookup");
    [PublishedArcCoreFact] void should_not_execute_using_the_service_provider_fallback() => _body.GetProperty("isSuccess").GetBoolean().ShouldBeFalse();
    [PublishedArcCoreFact] void should_not_misrepresent_the_refusal_as_unauthorized() => _body.GetProperty("isAuthorized").GetBoolean().ShouldBeTrue();
}
