// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A session cookie plus a v1 envelope are two credentials.</summary>
public class with_session_cookie_and_v1_envelope : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "x-cratis-forwarded-identity-signature"]);
        _raw.GetHeaderValues("Authorization").Returns([]);
        _raw.GetHeaderValues("Cookie").Returns(["cratis-identity-session=fixture"]);
        _raw.GetHeaderValues("x-cratis-forwarded-identity-signature").Returns(["v1.synthetic.signature"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
