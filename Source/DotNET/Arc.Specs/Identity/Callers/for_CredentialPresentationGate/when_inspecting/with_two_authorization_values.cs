// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_two_authorization_values : given.an_aspnet_request
{
    void Establish() => _http.Request.Headers.Authorization = new StringValues(["Bearer fixture-a", "Bearer fixture-b"]);
    void Because() => _result = new CredentialPresentationGate().Inspect(_request);

    [Fact] void should_preserve_both_values() => _result.Authorization.Count.ShouldEqual(2);
    [Fact] void should_refuse_ambiguous_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
