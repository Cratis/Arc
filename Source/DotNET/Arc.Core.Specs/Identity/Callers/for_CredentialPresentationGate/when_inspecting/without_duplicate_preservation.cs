// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class without_duplicate_preservation : given.a_gate
{
    void Establish() => ((ICanPreserveCallerCredentialHeaders)_request).PreservesDuplicateCredentialHeaders.Returns(false);
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_fail_closed_on_a_lossy_host() => _result.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
}
