// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_QueryTenantIdResolver.when_resolving;

public class and_query_parameter_exists : given.a_query_tenant_id_resolver
{
    const string expected_tenant_id = "test-tenant";
    string _result;

    void Establish()
    {
        _query["tenantId"] = expected_tenant_id;
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_tenant_id_from_query_parameter() => _result.ShouldEqual(expected_tenant_id);
}
