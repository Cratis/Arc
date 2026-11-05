// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_a_dpop_scheme : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Authorization", "DPoP"]);
        _raw.GetHeaderValues("Authorization").Returns(["DPoP synthetic-fixture"]);
        _raw.GetHeaderValues("DPoP").Returns(["synthetic-proof"]);
    }
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_the_unsupported_v1_scheme() => _result.Refusal.ShouldEqual(CallerRefusal.MalformedAuthorization);
    [Fact] void should_report_a_bad_request() => _result.Refusal!.Value.HttpStatus().ShouldEqual(400);
}
