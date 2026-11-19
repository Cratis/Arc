// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver;

public class when_endpoint_routing_changes_during_verification : given.a_resolver
{
    readonly TaskCompletionSource<CredentialVerification> _verification = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CallerResolution _repeated;

    void Establish()
    {
        _source.Shape.Returns(PresentedCredentialShape.ServiceAccount);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(_verification.Task);
        _request.Items[ExpectedCredentialShape.Key] = new ExpectedCredentialShape(PresentedCredentialShape.ServiceAccount);
    }

    async Task Because()
    {
        var pending = _resolver.GetCurrent();
        _request.Items.Remove(ExpectedCredentialShape.Key);
        _verification.SetResult(CredentialVerification.Verified(verified_facts.Service() with
        {
            PrincipalKind = CallerPrincipalKind.ClusterWorkload,
            CredentialKind = CallerCredentialKind.Workload
        }));
        _result = await pending;
        _request.Items[ExpectedCredentialShape.Key] = new ExpectedCredentialShape(PresentedCredentialShape.ServiceAccount);
        _repeated = await _resolver.GetCurrent();
    }

    [Fact] void should_poison_the_in_flight_resolution() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_not_restore_authority_when_metadata_is_restored() => _repeated.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_never_supply_a_caller() => _result.Caller.ShouldBeNull();
}
