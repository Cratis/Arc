// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.given;

public class a_tenant_id_accessor : Specification
{
    protected TenantIdAccessor _accessor;
    protected ITenantIdResolver _tenantIdResolver;

    void Establish()
    {
        _tenantIdResolver = Substitute.For<ITenantIdResolver>();
        _accessor = new TenantIdAccessor(_tenantIdResolver);
    }
}
