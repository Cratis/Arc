// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_envelope_and_bearer : given.an_aspnet_request
{
    void Establish()
    {
        _http.Request.Headers.Authorization = "Bearer fixture-a";
        _http.Request.Headers["x-cratis-forwarded-identity-signature"] = "v1.synthetic.signature";
    }
    void Because() => _result = new CredentialPresentationGate().Inspect(_request);

    [Fact] void should_refuse_both_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
}
