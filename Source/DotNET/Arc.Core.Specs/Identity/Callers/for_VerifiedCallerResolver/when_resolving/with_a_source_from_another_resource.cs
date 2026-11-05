// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_source_from_another_resource : given.a_resolver
{
    void Establish() => _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(verified_facts.Person() with { Resource = "https://other.example.test" }));
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_other_resource_facts() => _result.Refusal.ShouldEqual(CallerRefusal.ResourceMismatch);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
