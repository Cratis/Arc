// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_there_is_no_principal : given.a_tenant_membership_verification
{
    void Establish() => _principals.Current.Returns((ClaimsPrincipal)null);
    void Because() => _verification.Verify(_tenant);
    [Fact] void should_pass_an_unauthenticated_principal() => _membership.Received(1).IsMember(Arg.Is<ClaimsPrincipal>(principal => principal.Identity != null && !principal.Identity.IsAuthenticated), _tenant);
}
