// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_inputs_changed_during_verification : given.a_resolver
{
    CallerResolution _subsequent;
    async Task Because()
    {
        var pending = new TaskCompletionSource<CredentialVerification>(TaskCreationOptions.RunContinuationsAsynchronously);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var resolution = _resolver.GetCurrent();
        _raw.GetHeaderValues("Authorization").Returns(["Bearer changed-fixture"]);
        pending.SetResult(CredentialVerification.Verified(verified_facts.Person()));
        _result = await resolution;
        _subsequent = await _resolver.GetCurrent();
    }
    [Fact] void should_refuse_the_pending_resolution() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_refuse_subsequent_resolution() => _subsequent.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
