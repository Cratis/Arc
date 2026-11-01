// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_resolution_failed_earlier : given.a_tenant_membership_verification
{
    Exception _exception;
    bool _second;

    void Establish() => _services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(_ => throw new InvalidOperationException("Unavailable"), _ => new[] { _membership });

    void Because()
    {
        _exception = Catch.Exception(() => _verification.Verify(_tenant));
        _second = _verification.Verify(_tenant);
    }

    [Fact] void should_deny_the_first_check() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_not_remember_the_failure() => _second.ShouldBeTrue();
}
