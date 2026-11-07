// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Principals-and-workload row 28 on the ASP.NET host.</summary>
public class with_session_cookie_and_bearer : given.an_aspnet_request
{
    void Establish()
    {
        _http.Request.Headers.Cookie = "theme=dark; cratis-identity-session=fixture";
        _http.Request.Headers.Authorization = "Bearer fixture";
    }

    void Because() => _result = new CredentialPresentationGate().Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
