// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class after_a_tool_source_refusal : given.a_resolver
{
    CallerResolution _cached;
    CallerResolution _nextRequest;

    void Establish() => _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Refused(CallerRefusal.SourceRefused));

    async Task Because()
    {
        _result = await _resolver.GetCurrent();
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(verified_facts.Person()));
        _cached = await _resolver.GetCurrent();
        var next = NewRequest();
        _requests.Current.Returns(next);
        _nextRequest = await _resolver.GetCurrent();
    }

    [Fact] void should_keep_the_original_refusal() => _cached.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_reuse_the_refusal_resolution() => ReferenceEquals(_cached, _result).ShouldBeTrue();
    [Fact] void should_not_retry_with_weaker_authority() => _cached.Caller.ShouldBeNull();
    [Fact] void should_resolve_a_new_request_independently() => _nextRequest.Caller!.Person.ShouldEqual(verified_facts.Person().Person);
    [Fact] async Task should_verify_once_per_request() => await _source.Received(2).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
