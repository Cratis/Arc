// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_independent_emission;

public class and_the_captured_tenant_is_not_set : given.a_tenant_id_accessor
{
    TenantId _captured;
    TenantId _inside;
    TenantId _after;

    void Establish()
    {
        _tenantIdResolver.Resolve().Returns(string.Empty);
        _captured = _accessor.Current;
        _tenantIdResolver.Resolve().Returns("producer");
    }

    void Because()
    {
        using (_accessor.Begin("explicit-producer"))
        {
            using (TenantIdAccessor.Independent.BeginEmission(_captured))
            {
                _inside = _accessor.Current;
            }
            _after = _accessor.Current;
        }
    }

    [Fact] void should_keep_the_captured_emission_tenant_unset() => _inside.ShouldEqual(TenantId.NotSet);
    [Fact] void should_restore_the_producer_tenant() => _after.ShouldEqual(new TenantId("explicit-producer"));
    [Fact] void should_not_resolve_under_the_producer_identity() => _tenantIdResolver.Received(1).Resolve();
}
