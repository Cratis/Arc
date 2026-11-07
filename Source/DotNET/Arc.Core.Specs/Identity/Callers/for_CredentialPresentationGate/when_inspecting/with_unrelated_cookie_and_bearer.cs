// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Unrelated cookies, substring names and cookie values that look like bearers are not credentials.</summary>
public class with_unrelated_cookie_and_bearer : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["theme=dark; xcratis-identity-session=1; note=Bearer x; =; ;;"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_not_refuse() => _result.Refusal.ShouldBeNull();
    [Fact] void should_select_the_oauth_bearer_shape() => _result.Shape.ShouldEqual(PresentedCredentialShape.OAuthBearer);
}
