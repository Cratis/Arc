// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class without_credentials : given.a_gate
{
    void Establish()
    {
        _raw.HeaderNames.Returns(Array.Empty<string>());
        _raw.GetHeaderValues("Authorization").Returns(Array.Empty<string>());
    }
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_the_request() => _result.Refusal.ShouldEqual(CallerRefusal.NoCredential);
    [Fact] void should_not_select_a_credential_shape() => _result.Shape.ShouldBeNull();
}
