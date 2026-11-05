// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_spoofed_credential_and_actor_headers : given.a_resolver
{
    void Establish()
    {
        _raw.GetHeaderValues("Authorization").Returns([]);
        _raw.HeaderNames.Returns(["x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-actor", "x-cratis-grant", "x-cratis-on-behalf-session", "DPoP", "Cookie"]);
        _raw.RawTarget.Returns("/mcp?grant=untrusted-grant");
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_not_promote_unsigned_metadata() => _result.Refusal.ShouldEqual(CallerRefusal.NoCredential);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] async Task should_not_call_a_source() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
