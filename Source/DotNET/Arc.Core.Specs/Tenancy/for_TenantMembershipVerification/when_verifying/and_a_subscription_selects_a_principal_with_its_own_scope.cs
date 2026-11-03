// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_a_subscription_selects_a_principal_with_its_own_scope : given.a_membership_verification_with_real_scopes
{
    void Because()
    {
        BuildRoot(ServiceLifetime.Transient);
        var tenant = new TenantId("t");
        var subscription = Subscription(_root, "alice");
        _requests.Current = subscription;
        _verification.Verify(tenant);
        _verification.Verify(tenant);
        using (subscription.BeginSelectedPrincipal(Principal("bob"), NewScope().ServiceProvider))
        {
            _verification.Verify(tenant);
            _verification.Verify(tenant);
        }

        _verification.Verify(tenant);
        _verification.Verify(tenant);
    }

    [Fact] void should_resolve_once_per_provider_in_use() => _created.Count.ShouldEqual(3);
    [Fact] void should_use_the_root_instance_only_while_the_root_is_in_use() => _created.ToArray()[0].Calls.Count.ShouldEqual(2);
    [Fact] void should_use_the_owned_scope_instance_only_during_the_selection() => _created.ToArray()[1].Calls.Count.ShouldEqual(2);
    [Fact] void should_use_a_fresh_instance_after_the_selection_ends() => _created.ToArray()[2].Calls.Count.ShouldEqual(2);
    [Fact] void should_pass_the_selected_principal_during_the_selection() => _created.ToArray()[1].Calls.All(_ => _.Principal == "bob").ShouldBeTrue();
}
