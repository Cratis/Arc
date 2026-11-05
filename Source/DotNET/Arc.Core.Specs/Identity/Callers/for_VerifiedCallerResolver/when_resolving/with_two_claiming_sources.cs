// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_two_claiming_sources : given.a_resolver
{
    void Establish() => _sources.Add(_source);
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_ambiguous_ownership() => _result.Refusal.ShouldEqual(CallerRefusal.SourceConflict);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
    [Fact] async Task should_not_invoke_conflicting_sources() => await _source.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
