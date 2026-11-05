// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_an_ordinary_query : given.a_gate
{
    void Establish() => _raw.RawTarget.Returns("/mcp?filter=active&tenant=fixture");
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_accept_the_single_presentation() => _result.Refusal.ShouldBeNull();
    [Fact] void should_keep_the_bearer() => _result.Authorization.Count.ShouldEqual(1);
}
