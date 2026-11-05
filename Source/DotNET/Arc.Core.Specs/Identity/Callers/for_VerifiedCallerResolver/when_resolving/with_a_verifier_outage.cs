// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_verifier_outage : given.a_resolver
{
    void Establish() => _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<CredentialVerification>(new InvalidOperationException("synthetic outage")));
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_fail_closed() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
