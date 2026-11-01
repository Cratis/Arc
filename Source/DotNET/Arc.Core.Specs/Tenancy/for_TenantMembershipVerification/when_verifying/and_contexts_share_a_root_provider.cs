// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_contexts_share_a_root_provider : given.a_tenant_membership_verification
{
    IHttpRequestContext _otherRequest;
    ITenantMembership _otherMembership;
    ServiceProvider _root;
    int _resolutions;

    void Establish()
    {
        _otherMembership = Substitute.For<ITenantMembership>();
        _otherMembership.IsMember(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(true);
        var services = new ServiceCollection();
        services.AddTransient(_ => ++_resolutions == 1 ? _membership : _otherMembership);
        _root = services.BuildServiceProvider();
        _request.RequestServices.Returns(_root);
        _otherRequest = Substitute.For<IHttpRequestContext>();
        _otherRequest.RequestServices.Returns(_root);
        _otherRequest.Items.Returns(new Dictionary<object, object?>());
    }

    void Because()
    {
        _verification.Verify(_tenant);
        _requests.Current.Returns(_otherRequest);
        _verification.Verify(_tenant);
    }

    void Destroy() => _root.Dispose();

    [Fact] void should_resolve_memberships_for_each_context() => _resolutions.ShouldEqual(2);
    [Fact] void should_not_reuse_instances_across_contexts() => _membership.Received(1).IsMember(_principal, _tenant);
    [Fact] void should_use_the_second_contexts_instances() => _otherMembership.Received(1).IsMember(_principal, _tenant);
}
