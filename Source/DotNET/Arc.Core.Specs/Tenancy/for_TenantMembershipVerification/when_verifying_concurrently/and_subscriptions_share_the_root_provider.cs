// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying_concurrently;

public class and_subscriptions_share_the_root_provider : given.a_membership_verification_with_real_scopes
{
    const int Checks = 200;

    void Because()
    {
        BuildRoot(ServiceLifetime.Transient);
        VerifyInterleaved(
            Subscription(_root, "alice"),
            new TenantId("tenant-a"),
            Subscription(_root, "bob"),
            new TenantId("tenant-b"),
            Checks);
    }

    [Fact] void should_resolve_once_per_context() => _created.Count.ShouldEqual(2);
    [Fact] void should_use_each_instance_for_every_check_of_its_context() => _created.All(_ => _.Calls.Count == Checks).ShouldBeTrue();
    [Fact] void should_never_mix_principals_or_tenants() => _created.Select(_ => _.Calls.Distinct().Single()).OrderBy(_ => _.Principal).ToArray().ShouldEqual([("alice", "tenant-a"), ("bob", "tenant-b")]);
}
