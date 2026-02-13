// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_QueryTenantIdResolver.when_resolving;

public class and_custom_query_parameter_is_configured : given.a_query_tenant_id_resolver
{
    const string CustomParameterName = "tenant";
    const string ExpectedTenantId = "custom-tenant";
    string _result;

    void Establish()
    {
        _options.Value.Tenancy.QueryParameter = CustomParameterName;
        _query[CustomParameterName] = ExpectedTenantId;
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_tenant_id_from_custom_query_parameter() => _result.ShouldEqual(ExpectedTenantId);
}
