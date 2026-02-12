// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_HeaderTenantIdResolver.when_resolving;

public class and_custom_header_name_is_configured : given.a_header_tenant_id_resolver
{
    const string custom_header_name = "X-Custom-Tenant";
    const string expected_tenant_id = "custom-tenant";
    string _result;

    void Establish()
    {
        _options.Value.Tenancy.HttpHeader = custom_header_name;
        _headers[custom_header_name] = expected_tenant_id;
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_tenant_id_from_custom_header() => _result.ShouldEqual(expected_tenant_id);
}
