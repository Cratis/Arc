// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_a_pat_namespace : given.a_gate
{
    void Establish() => _raw.GetHeaderValues("Authorization").Returns(["bEaReR Product-1_PAT_fixture=="]);
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_dispatch_only_to_a_pat_source() => _result.Shape.ShouldEqual(PresentedCredentialShape.PatBearer);
    [Fact] void should_not_refuse_valid_b64token_syntax() => _result.Refusal.ShouldBeNull();
}
