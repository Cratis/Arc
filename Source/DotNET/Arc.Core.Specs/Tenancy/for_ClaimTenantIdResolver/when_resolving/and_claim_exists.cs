// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_ClaimTenantIdResolver.when_resolving;

public class and_claim_exists : given.a_claim_tenant_id_resolver
{
    const string ExpectedTenantId = "test-tenant";
    string _result;

    void Establish()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("tenant_id", ExpectedTenantId));
        _user = new ClaimsPrincipal(identity);
        _context.User.Returns(_user);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_tenant_id_from_claim() => _result.ShouldEqual(ExpectedTenantId);
}
