// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_refused_source : given.a_resolver
{
    IVerifiedCredentialSource _fallback;

    void Establish()
    {
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Refused(CallerRefusal.SourceRefused));
        _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture_pat_secret"]);
        _source.Shape.Returns(PresentedCredentialShape.PatBearer);
        _fallback = Substitute.For<VerifiedCredentialSourceForSpecs>();
        _fallback.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        _sources.Add(_fallback);
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_keep_the_refusal() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] async Task should_not_try_a_weaker_fallback() => await _fallback.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
