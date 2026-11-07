// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Cookies that merely resemble a configured name are not credentials.</summary>
public class with_unrelated_cookie_and_configured_names : given.a_gate
{
    void Establish()
    {
        var names = Substitute.For<IAdditionalSessionCookieNames>();
        names.Names.Returns(["my-app-session"]);
        _configured = new(additionalSessionCookieNames: names);
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["xmy-app-session=1; my-app-sessionX=2; my-app-sessionC=3; theme=dark"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    CredentialPresentationGate _configured;

    void Because() => _result = _configured.Inspect(_request);

    [Fact] void should_not_refuse() => _result.Refusal.ShouldBeNull();
    [Fact] void should_select_the_oauth_bearer_shape() => _result.Shape.ShouldEqual(PresentedCredentialShape.OAuthBearer);
}
