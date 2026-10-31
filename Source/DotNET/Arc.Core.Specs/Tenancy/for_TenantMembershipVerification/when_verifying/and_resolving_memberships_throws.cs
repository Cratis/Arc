// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_resolving_memberships_throws : given.a_tenant_membership_verification
{
    Exception _exception;
    void Establish() => _services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(_ => throw new InvalidOperationException("Unavailable"));
    void Because() => _exception = Catch.Exception(() => _verification.Verify(_tenant));
    [Fact] void should_deny_membership() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
}
