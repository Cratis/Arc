// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_verified_facts : given.a_resolver
{
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_construct_the_caller() => _result.Caller.ShouldNotBeNull();
    [Fact] void should_have_no_refusal() => _result.Refusal.ShouldBeNull();
    [Fact] async Task should_verify_once() => await _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
