// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_request_services_change : given.a_tenant_membership_verification
{
    IServiceProvider _otherServices;
    ITenantMembership _otherMembership;

    void Establish()
    {
        _otherServices = Substitute.For<IServiceProvider>();
        _otherMembership = Substitute.For<ITenantMembership>();
        _otherMembership.IsMember(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(true);
        _otherServices.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { _otherMembership });
    }

    void Because()
    {
        _verification.Verify(_tenant);
        _request.RequestServices.Returns(_otherServices);
        _verification.Verify(_tenant);
    }

    [Fact] void should_use_the_new_request_services_memberships() => _otherMembership.Received(1).IsMember(_principal, _tenant);
    [Fact] void should_not_use_the_previous_memberships_again() => _membership.Received(1).IsMember(_principal, _tenant);
}
