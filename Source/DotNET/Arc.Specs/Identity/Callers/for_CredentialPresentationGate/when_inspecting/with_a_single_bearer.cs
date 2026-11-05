// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_a_single_bearer : given.an_aspnet_request
{
    void Establish() => _http.Request.Headers.Authorization = "Bearer fixture-a";
    void Because() => _result = new CredentialPresentationGate().Inspect(_request);

    [Fact] void should_accept_one_presentation_for_later_verification() => _result.Refusal.ShouldBeNull();
    [Fact] void should_not_authenticate_it() => _http.User.Identity.IsAuthenticated.ShouldBeFalse();
}
