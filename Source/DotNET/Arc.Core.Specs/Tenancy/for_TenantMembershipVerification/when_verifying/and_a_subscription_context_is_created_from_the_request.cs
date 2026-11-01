// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_a_subscription_context_is_created_from_the_request : given.a_membership_verification_with_real_scopes
{
    void Because()
    {
        BuildRoot(ServiceLifetime.Transient);
        var request = Request(Principal("alice"));
        request.RequestServices.Returns(_root);
        _requests.Current = request;
        _verification.Verify(new TenantId("t"));
        _requests.Current = Subscription(request, _root);
        _verification.Verify(new TenantId("t"));
    }

    [Fact] void should_resolve_memberships_for_each_context() => _created.Count.ShouldEqual(2);
    [Fact] void should_use_each_instance_once() => _created.All(_ => _.Calls.Count == 1).ShouldBeTrue();
}
