// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Tenancy.for_TenantNamespaceResolver.when_resolving;

/// <summary>
/// Naming the default tenant explicitly has to land on the same namespace as resolving no tenant at all —
/// otherwise the write side and the tenant-partitioned read side address different databases.
/// </summary>
public class and_tenant_id_is_the_default_tenant : Specification
{
    TenantNamespaceResolver _resolver;
    ITenantIdAccessor _tenantIdAccessor;
    EventStoreNamespaceName _result;

    void Establish()
    {
        _tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        _tenantIdAccessor.Current.Returns(TenantId.Default);

        _resolver = new TenantNamespaceResolver(_tenantIdAccessor);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
