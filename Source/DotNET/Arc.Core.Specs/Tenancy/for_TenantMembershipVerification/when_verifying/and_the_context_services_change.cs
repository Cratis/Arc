// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_context_services_change : given.a_tenant_membership_verification
{
    IServiceProvider _otherServices;
    ITenantMembership _otherMembership;

    void Establish()
    {
        _otherMembership = Substitute.For<ITenantMembership>();
        _otherMembership.IsMember(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(true);
        _otherServices = Substitute.For<IServiceProvider>();
        _otherServices.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { _otherMembership });
    }

    void Because()
    {
        _verification.Verify(_tenant);
        _request.RequestServices.Returns(_otherServices);
        _verification.Verify(_tenant);
    }

    [Fact] void should_resolve_from_the_first_services_once() => _services.Received(1).GetService(typeof(IEnumerable<ITenantMembership>));
    [Fact] void should_resolve_from_the_new_services_once() => _otherServices.Received(1).GetService(typeof(IEnumerable<ITenantMembership>));
    [Fact] void should_use_the_first_instances_only_before_the_change() => _membership.Received(1).IsMember(_principal, _tenant);
    [Fact] void should_use_the_new_instances_after_the_change() => _otherMembership.Received(1).IsMember(_principal, _tenant);
}
