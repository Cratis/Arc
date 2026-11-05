// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_unknown_duplicate_preservation : given.a_gate
{
    void Establish() => _request = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest>();
    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_require_an_explicit_host_guarantee() => _result.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
}
