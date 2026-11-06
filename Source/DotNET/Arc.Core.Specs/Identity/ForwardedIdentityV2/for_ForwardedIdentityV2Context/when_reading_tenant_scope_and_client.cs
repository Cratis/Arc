// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Context;

public class when_reading_tenant_scope_and_client : Specification
{
    ForwardedIdentityV2Context _present;
    ForwardedIdentityV2Context _absent;
    ForwardedIdentityV2Context _deduplicated;

    void Because()
    {
        var vector = VerificationFixture.DirectBrowser();
        var headers = vector.Inputs.Headers.ToDictionary(header => header.Name.ToLowerInvariant(), header => header.Value.Trim(' ', '\t'));
        headers["x-cratis-tenant-id"] = "signed-tenant";
        headers["tenant-id"] = "alias-tenant";
        headers["custom-tenant"] = "custom-target";
        headers["x-cratis-token-scope"] = "example:read example:write";
        headers["x-cratis-token-client-id"] = "declared-client";
        _present = ForwardedIdentityV2Context.Create(headers, vector.Inputs.RealmIssuer, "Custom-Tenant");
        _deduplicated = ForwardedIdentityV2Context.Create(headers, vector.Inputs.RealmIssuer, "TENANT-ID");
        foreach (var name in new[] { "x-cratis-tenant-id", "tenant-id", "custom-tenant", "x-cratis-token-scope", "x-cratis-token-client-id" }) headers.Remove(name);
        _absent = ForwardedIdentityV2Context.Create(headers, vector.Inputs.RealmIssuer, "Custom-Tenant");
    }

    [Fact] void should_copy_raw_tenant_targets_in_order() => _present.TenantTargets.ShouldEqual<string>("signed-tenant", "alias-tenant", "custom-target");
    [Fact] void should_deduplicate_alias_names() => _deduplicated.TenantTargets.ShouldEqual<string>("signed-tenant", "alias-tenant");
    [Fact] void should_copy_scope() => _present.TokenScope.ShouldEqual("example:read example:write");
    [Fact] void should_copy_client() => _present.TokenClientId.ShouldEqual("declared-client");
    [Fact] void should_leave_absent_targets_empty() => _absent.TenantTargets.ShouldBeEmpty();
    [Fact] void should_leave_absent_scope_null() => _absent.TokenScope.ShouldBeNull();
    [Fact] void should_leave_absent_client_null() => _absent.TokenClientId.ShouldBeNull();
}
