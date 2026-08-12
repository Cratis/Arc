// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_FixedTenantIdResolver.when_resolving;

public class and_the_tenant_id_is_configured_under_the_development_name : given.a_fixed_tenant_id_resolver
{
    const string CustomTenantId = "acme-legacy-name";
    string _result;

    void Establish()
    {
        _options.Value.Tenancy.DevelopmentTenantId = CustomTenantId;
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_tenant_id_configured_under_the_development_name() => _result.ShouldEqual(CustomTenantId);
}
