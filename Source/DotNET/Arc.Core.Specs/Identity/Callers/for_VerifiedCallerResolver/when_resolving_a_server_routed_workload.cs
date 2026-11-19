// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver;

public class when_resolving_a_server_routed_workload : given.a_resolver
{
    void Establish()
    {
        _source.Shape.Returns(PresentedCredentialShape.ServiceAccount);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(
            verified_facts.Service() with { PrincipalKind = CallerPrincipalKind.ClusterWorkload, CredentialKind = CallerCredentialKind.Workload }));
        _request.Items[ExpectedCredentialShape.Key] = new ExpectedCredentialShape(PresentedCredentialShape.ServiceAccount);
    }

    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_construct_the_verified_workload() => _result.Caller!.PrincipalKind.ShouldEqual(CallerPrincipalKind.ClusterWorkload);
    [Fact] void should_not_run_person_tenant_resolution() => _ = _tenants.DidNotReceive().Current;
    [Fact] void should_not_inherit_the_independent_tenant_header() => _result.Caller!.Tenant.ShouldBeNull();
    [Fact] void should_dispatch_only_the_server_selected_shape() => _source.Received(1).Verify(Arg.Is<CredentialPresentation>(value => value.Shape == PresentedCredentialShape.ServiceAccount), Arg.Any<CancellationToken>());
}
