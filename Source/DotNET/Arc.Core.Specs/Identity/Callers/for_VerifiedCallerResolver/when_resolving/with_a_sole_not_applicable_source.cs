// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_sole_not_applicable_source : given.a_resolver
{
    void Establish() => _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.NotApplicable());
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_report_no_matching_source() => _result.Refusal.ShouldEqual(CallerRefusal.NoMatchingSource);
    [Fact] void should_not_build_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] async Task should_verify_exactly_once() => await _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
