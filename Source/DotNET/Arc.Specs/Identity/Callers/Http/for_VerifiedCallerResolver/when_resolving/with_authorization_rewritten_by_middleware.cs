// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.Http.for_VerifiedCallerResolver.when_resolving;

public class with_authorization_rewritten_by_middleware : given.a_wire_host
{
    async Task Because() => await ResolveWire("Authorization: Bearer synthetic-fixture\r\n", rewriteAuthorization: true);
    [Fact] void should_refuse() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
