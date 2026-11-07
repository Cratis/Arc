// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Lenient servers accept a comma as a cookie pair separator, so the gate must too.</summary>
public class with_comma_separated_cookie_and_bearer : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["a=1, .AspNetCore.Cookies=x"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
