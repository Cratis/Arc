// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A host-supplied cookie name, in any case and including numbered chunks, counts as a session credential.</summary>
public class with_configured_cookie_name_and_bearer : given.a_gate
{
    void Establish()
    {
        var names = Substitute.For<IAdditionalSessionCookieNames>();
        names.Names.Returns(["my-app-session"]);
        _configured = new(additionalSessionCookieNames: names);
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns(["theme=dark, MY-APP-SESSIONC2=fixture"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    CredentialPresentationGate _configured;

    void Because() => _result = _configured.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
