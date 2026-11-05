// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.Http.for_VerifiedCallerResolver.when_resolving;

public class with_two_authorization_lines : given.a_wire_host
{
    async Task Because() => await ResolveWire("Authorization: Bearer fixture-a\r\nauthorization: Bearer fixture-b\r\n");

    [Fact] void should_preserve_both_wire_lines() => _authorizationValues.ShouldEqual(2);
    [Fact] void should_refuse_multiple_credentials() => _result.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_not_build_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] async Task should_not_invoke_a_source() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
