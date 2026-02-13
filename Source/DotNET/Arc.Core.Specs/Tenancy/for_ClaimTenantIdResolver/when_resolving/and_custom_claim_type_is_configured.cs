// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_ClaimTenantIdResolver.when_resolving;

public class and_custom_claim_type_is_configured : given.a_claim_tenant_id_resolver
{
    const string CustomClaimType = "custom_tenant";
    const string ExpectedTenantId = "custom-tenant";
    string _result;

    void Establish()
    {
        _options.Value.Tenancy.ClaimType = CustomClaimType;
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(CustomClaimType, ExpectedTenantId));
        _user = new ClaimsPrincipal(identity);
        _context.User.Returns(_user);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_tenant_id_from_custom_claim() => _result.ShouldEqual(ExpectedTenantId);
}
