// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A cookie that no registered scheme names stays an ordinary cookie.</summary>
public class with_unrelated_cookie_over_cookie_schemes : given.a_gate_over_cookie_schemes
{
    void Establish()
    {
        _http.Request.Headers.Cookie = "theme=dark; other-session=fixture";
        _http.Request.Headers.Authorization = "Bearer fixture";
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_not_refuse() => _result.Refusal.ShouldBeNull();
}
