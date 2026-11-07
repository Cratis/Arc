// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A v2 envelope signs the session, so a cookie, a covered bearer and a v2 envelope are one credential.</summary>
public class with_session_cookie_and_v2_envelope : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization", "x-cratis-forwarded-identity-signature"]);
        _raw.GetHeaderValues("Cookie").Returns(["cratis-identity-session=fixture"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
        _raw.GetHeaderValues("x-cratis-forwarded-identity-signature").Returns(["v2.synthetic.signature"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_not_refuse() => _result.Refusal.ShouldBeNull();
    [Fact] void should_select_the_envelope_shape() => _result.Shape.ShouldEqual(PresentedCredentialShape.Envelope);
}
