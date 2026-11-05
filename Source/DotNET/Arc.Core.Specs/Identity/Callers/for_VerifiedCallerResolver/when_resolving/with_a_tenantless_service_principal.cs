// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_tenantless_service_principal : given.a_resolver
{
    void Establish()
    {
        _tenants.Current.Returns(TenantId.NotSet);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(verified_facts.Service()));
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_not_refuse_the_absent_tenant() => _result.Refusal.ShouldBeNull();
    [Fact] void should_build_the_service_caller() => _result.Caller.PrincipalKind.ShouldEqual(CallerPrincipalKind.OrganizationAutomation);
    [Fact] void should_not_promote_the_sentinel_to_a_tenant() => _result.Caller.Tenant.ShouldBeNull();
}
