// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Principals-and-workload row 28: a session cookie plus a bearer are two credentials.</summary>
public class with_session_cookie_and_bearer : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["cratis-identity-session=fixture; other=1"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_not_select_a_shape() => _result.Shape.ShouldBeNull();
    [Fact] void should_preserve_the_authorization_set() => _result.Authorization.Count.ShouldEqual(1);
}
