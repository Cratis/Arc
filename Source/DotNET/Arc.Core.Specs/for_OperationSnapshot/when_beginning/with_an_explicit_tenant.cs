// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class with_an_explicit_tenant : given.an_operation_snapshot_environment
{
    TenantId _tenant;
    TenantProvenance _provenance;

    void Because()
    {
        ITenantScope scope = _accessor;
        using (scope.Begin("explicit"))
        {
            using var snapshot = OperationSnapshot.Begin(_services, () => null);
            _tenant = snapshot.Tenant;
            _provenance = snapshot.TenantProvenance;
        }
    }

    [Fact] void should_use_the_explicit_tenant() => _tenant.ShouldEqual(new TenantId("explicit"));
    [Fact] void should_say_it_is_explicit() => _provenance.ShouldEqual(TenantProvenance.Explicit);
    [Fact] void should_not_call_the_resolver() => _resolver.DidNotReceive().Resolve();
}
