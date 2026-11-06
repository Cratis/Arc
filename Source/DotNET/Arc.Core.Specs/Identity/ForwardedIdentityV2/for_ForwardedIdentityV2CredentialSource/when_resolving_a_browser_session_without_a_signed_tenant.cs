// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_a_browser_session_without_a_signed_tenant : given.a_browser_session_resolution
{
    CallerResolution _resolved;
    CallerResolution _tenantless;
    async Task Because()
    {
        var vector = VerificationFixture.DirectBrowser();
        foreach (var name in new[] { "x-cratis-tenant-id", "tenant-id", vector.Inputs.TenantHeader }.Where(name => name is not null).Distinct())
        {
            vector = VerificationFixture.Header(vector, name, null);
        }
        _resolved = await Resolve(vector, new("tenant-test"), new());
        _tenantless = await Resolve(vector, TenantId.NotSet, new());
    }
    [Fact] void should_not_fill_in_the_signed_tenant() => _resolved.Refusal.ShouldEqual(CallerRefusal.TenantMismatch);
    [Fact] void should_have_no_caller_for_a_resolved_tenant() => _resolved.Caller.ShouldBeNull();
    [Fact] void should_accept_the_tenantless_control() => _tenantless.Refusal.ShouldBeNull();
    [Fact] void should_have_no_tenant() => _tenantless.Caller!.Tenant.ShouldBeNull();
}
