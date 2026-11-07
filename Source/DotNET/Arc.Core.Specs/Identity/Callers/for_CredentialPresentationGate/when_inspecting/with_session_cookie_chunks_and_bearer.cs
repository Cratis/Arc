// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>A chunked default authentication cookie counts as a session cookie; a non-numeric chunk suffix does not.</summary>
public class with_session_cookie_chunks_and_bearer : given.a_gate
{
    CredentialPresentation _lookalike;

    void Establish()
    {
        _raw.HeaderNames.Returns(["Cookie", "Authorization"]);
        _raw.GetHeaderValues("Cookie").Returns([".AspNetCore.CookiesC2=fixture"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
    }

    void Because()
    {
        _result = _gate.Inspect(_request);
        _raw.GetHeaderValues("Cookie").Returns([".AspNetCore.CookiesCx=fixture; .AspNetCore.CookiesC=fixture"]);
        _lookalike = _gate.Inspect(_request);
    }

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_not_count_a_lookalike_chunk_name() => _lookalike.Refusal.ShouldBeNull();
}
