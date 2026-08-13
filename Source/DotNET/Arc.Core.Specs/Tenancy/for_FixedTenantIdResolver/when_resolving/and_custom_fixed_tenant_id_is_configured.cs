// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_FixedTenantIdResolver.when_resolving;

public class and_custom_fixed_tenant_id_is_configured : given.a_fixed_tenant_id_resolver
{
    const string CustomTenantId = "acme";
    string _result;

    void Establish()
    {
        _options.Value.Tenancy.FixedTenantId = CustomTenantId;
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_configured_fixed_tenant_id() => _result.ShouldEqual(CustomTenantId);
}
