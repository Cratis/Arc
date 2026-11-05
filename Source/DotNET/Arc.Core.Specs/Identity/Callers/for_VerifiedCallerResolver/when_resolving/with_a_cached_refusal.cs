// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_cached_refusal : given.a_resolver
{
    CallerResolution _again;

    void Establish() => _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture-a", "Bearer fixture-b"]);
    async Task Because()
    {
        _result = await _resolver.GetCurrent();
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture-c"]);
        _again = await _resolver.GetCurrent();
    }

    [Fact] void should_retain_the_first_refusal() => _again.Refusal.ShouldEqual(CallerRefusal.MultipleCredentials);
    [Fact] void should_reuse_the_resolution() => _again.ShouldBeSame(_result);
    [Fact] async Task should_never_verify_ambiguous_credentials() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
