// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver;

public class when_unsigned_headers_request_workload_routing : given.a_resolver
{
    void Establish()
    {
        _source.Shape.Returns(PresentedCredentialShape.ServiceAccount);
        _raw.HeaderNames.Returns(["Authorization", "X-Workload", "X-Expected-Credential-Shape", "X-Forwarded-User"]);
        _raw.GetHeaderValues("X-Workload").Returns(["service-fixture"]);
        _raw.GetHeaderValues("X-Expected-Credential-Shape").Returns(["ServiceAccount"]);
        _raw.GetHeaderValues("X-Forwarded-User").Returns(["privileged-person"]);
    }

    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_not_turn_unsigned_headers_into_routing_metadata() => _result.Refusal.ShouldEqual(CallerRefusal.NoMatchingSource);
    [Fact] void should_not_probe_the_workload_verifier() => _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
