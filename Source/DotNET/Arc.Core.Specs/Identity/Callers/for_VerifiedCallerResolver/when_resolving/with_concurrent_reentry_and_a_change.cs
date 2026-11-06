// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_concurrent_reentry_and_a_change : given.a_resolver
{
    Task<CallerResolution>[] _pending;
    CallerResolution[] _completed;
    async Task Because()
    {
        var verification = new TaskCompletionSource<CredentialVerification>(TaskCreationOptions.RunContinuationsAsynchronously);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(verification.Task);
        _pending = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Task.FromResult(_resolver.GetCurrent()))));
        verification.SetResult(CredentialVerification.Verified(verified_facts.Person()));
        _completed = await Task.WhenAll(_pending);
        _raw.GetHeaderValues("Authorization").Returns(["Bearer changed-fixture"]);
        _result = await _resolver.GetCurrent();
    }
    [Fact] void should_share_one_task() => _pending.All(task => ReferenceEquals(task, _pending[0])).ShouldBeTrue();
    [Fact] void should_accept_initial_resolution() => _completed.All(result => result.Caller is not null && result.Refusal is null).ShouldBeTrue();
    [Fact] void should_refuse_after_change() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller_after_change() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
