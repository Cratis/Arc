// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_not_applicable_source : given.a_resolver
{
    void Establish()
    {
        var unrelated = Substitute.For<IVerifiedCredentialSource>();
        unrelated.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.NotApplicable());
        unrelated.Shape.Returns(PresentedCredentialShape.PatBearer);
        _sources.Insert(0, unrelated);
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_allow_the_one_claiming_source() => _result.Caller.ShouldNotBeNull();
    [Fact] void should_not_treat_not_applicable_as_refused() => _result.Refusal.ShouldBeNull();
}
