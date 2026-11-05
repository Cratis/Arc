// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Vector 22.</summary>
public class with_two_bearers : given.a_gate
{
    void Establish() => _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture-a", "Bearer fixture-b"]);
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_preserve_the_presented_set() => _result.Authorization.Count.ShouldEqual(2);
}
