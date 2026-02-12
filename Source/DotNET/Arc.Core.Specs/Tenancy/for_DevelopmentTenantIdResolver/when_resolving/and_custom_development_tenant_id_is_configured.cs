// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_DevelopmentTenantIdResolver.when_resolving;

public class and_custom_development_tenant_id_is_configured : given.a_development_tenant_id_resolver
{
    const string custom_tenant_id = "custom-dev-tenant";
    string _result;

    void Establish()
    {
        _options.Value.Tenancy.DevelopmentTenantId = custom_tenant_id;
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_configured_development_tenant_id() => _result.ShouldEqual(custom_tenant_id);
}
