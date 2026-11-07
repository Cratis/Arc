// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Cookie names are matched case-insensitively (fail closed), so a differently cased name is still the session cookie.</summary>
public class with_cookie_name_case_difference : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["CRATIS-IDENTITY-SESSION=fixture"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
