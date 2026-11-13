// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_HeaderTenantIdResolver.when_resolving;

public class and_header_has_many_identical_values : given.a_header_tenant_id_resolver
{
    string _result;

    void Establish() => _headers[Constants.DefaultTenantIdHeader] = string.Join(',', Enumerable.Repeat("acme", 1000));
    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_single_tenant() => _result.ShouldEqual("acme");
}
