// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_spoofed_metadata_and_a_verified_pat : given.a_resolver
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["Authorization", "x-cratis-credential-id", "x-cratis-actor", "x-cratis-grant"]);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer product_pat_fixture"]);
        _raw.GetHeaderValues("x-cratis-credential-id").Returns(["untrusted-id"]);
        _raw.GetHeaderValues("x-cratis-actor").Returns(["untrusted-actor"]);
        _raw.RawTarget.Returns("/mcp?grant=untrusted-grant");
        _source.Shape.Returns(PresentedCredentialShape.PatBearer);
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_use_only_the_verified_credential_identity() => _result.Caller.CredentialId.ShouldEqual(verified_facts.Person().CredentialId);
    [Fact] void should_not_invent_an_actor() => _result.Caller.Actor.ShouldBeNull();
    [Fact] void should_not_invent_a_grant() => _result.Caller.GrantId.ShouldBeNull();
}
