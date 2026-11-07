// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>The session cookie in a second Cookie header, with surrounding whitespace and a quoted value, still counts.</summary>
public class with_repeated_cookie_headers_and_bearer : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["theme=dark", "  a=1;  cratis-identity-session=\"fixture\"  "]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
