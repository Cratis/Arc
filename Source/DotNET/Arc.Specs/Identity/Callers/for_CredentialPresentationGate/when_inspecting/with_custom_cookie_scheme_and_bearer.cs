// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A cookie named by a registered cookie authentication scheme counts as a session credential.</summary>
public class with_custom_cookie_scheme_and_bearer : given.a_gate_over_cookie_schemes
{
    void Establish()
    {
        _http.Request.Headers.Cookie = "theme=dark; MY-APP-SESSION=fixture";
        _http.Request.Headers.Authorization = "Bearer fixture";
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
