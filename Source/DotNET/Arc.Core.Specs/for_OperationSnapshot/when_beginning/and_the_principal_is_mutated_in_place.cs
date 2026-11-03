// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class and_the_principal_is_mutated_in_place : given.an_operation_snapshot_environment
{
    Exception _principalError;
    Exception _causedByError;
    Exception _tenantError;
    bool _membershipDenied;

    void Because()
    {
        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        ((ClaimsIdentity)_user.Identity!).AddClaim(new Claim(ClaimTypes.Role, "admin"));
        _causedByError = Catch.Exception(() => _ = snapshot.CausedBy);
        _principalError = Catch.Exception(() => _ = snapshot.Principal);
        _tenantError = Catch.Exception(() => _ = snapshot.Tenant);
        _membershipDenied = snapshot.MembershipDenied;
    }

    [Fact] void should_fail_closed_reading_the_caller() => _causedByError.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_fail_closed_reading_the_principal() => _principalError.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_fail_closed_reading_the_tenant() => _tenantError.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_mark_the_membership_as_denied() => _membershipDenied.ShouldBeTrue();
}
