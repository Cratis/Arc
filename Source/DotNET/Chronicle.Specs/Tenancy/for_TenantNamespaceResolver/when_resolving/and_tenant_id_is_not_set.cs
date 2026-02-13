// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Tenancy.for_TenantNamespaceResolver.when_resolving;

public class and_tenant_id_is_not_set : Specification
{
    TenantNamespaceResolver _resolver;
    ITenantIdAccessor _tenantIdAccessor;
    EventStoreNamespaceName _result;

    void Establish()
    {
        _tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        _tenantIdAccessor.Current.Returns(TenantId.NotSet);

        _resolver = new TenantNamespaceResolver(_tenantIdAccessor);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
