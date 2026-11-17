// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_QueryTenantIdResolver.when_resolving;

public class and_query_parameter_has_conflicting_values : given.a_query_tenant_id_resolver
{
    string _result;

    void Establish() => _query["tenantId"] = "acme,evil";
    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_to_no_tenant() => _result.ShouldEqual(string.Empty);
}
