// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Unrelated cookies do not count as credentials on the ASP.NET host.</summary>
public class with_unrelated_cookie_and_bearer : given.an_aspnet_request
{
    void Establish()
    {
        _http.Request.Headers.Cookie = "theme=dark";
        _http.Request.Headers.Authorization = "Bearer fixture";
    }

    void Because() => _result = new CredentialPresentationGate().Inspect(_request);

    [Fact] void should_not_refuse() => _result.Refusal.ShouldBeNull();
}
