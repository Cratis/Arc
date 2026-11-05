// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_a_dotted_namespace : given.a_gate
{
    void Establish() => _raw.GetHeaderValues("Authorization").Returns(["Bearer product.id_pat_fixture"]);
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_not_classify_dotted_prefixes_as_pat_namespaces() => _result.Shape.ShouldEqual(PresentedCredentialShape.OAuthBearer);
}
