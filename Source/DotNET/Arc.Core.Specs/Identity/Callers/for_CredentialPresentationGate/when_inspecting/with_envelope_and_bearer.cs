// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Vector 22; envelope coverage is not implemented in v1.</summary>
public class with_envelope_and_bearer : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Authorization", "x-cratis-forwarded-identity-signature"]);
        _raw.GetHeaderValues("x-cratis-forwarded-identity-signature").Returns(["v1.synthetic.signature"]);
    }
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
