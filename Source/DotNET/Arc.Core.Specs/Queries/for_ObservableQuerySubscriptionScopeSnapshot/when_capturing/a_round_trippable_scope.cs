// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionScopeSnapshot.when_capturing;

public class a_round_trippable_scope : Specification
{
    given.TenantScope _scope;
    ObservableQuerySubscriptionScopeSnapshot _snapshot;
    object _first;
    object _second;

    void Establish() => _scope = new("tenant", [1, 2]);

    void Because()
    {
        _snapshot = new(_scope, new ArcOptions().JsonSerializerOptions);
        _first = _snapshot.CreateScope();
        _second = _snapshot.CreateScope();
    }

    [Fact] void should_restore_the_runtime_type() => _first.ShouldBeOfExactType<given.TenantScope>();
    [Fact] void should_restore_the_tenant() => ((given.TenantScope)_first).Tenant.ShouldEqual("tenant");
    [Fact] void should_restore_the_regions() => ((given.TenantScope)_first).Regions.ShouldContainOnly(1, 2);
    [Fact] void should_not_hand_out_the_original_instance() => ReferenceEquals(_first, _scope).ShouldBeFalse();
    [Fact] void should_hand_out_an_independent_copy_each_time() => ReferenceEquals(_first, _second).ShouldBeFalse();
}
