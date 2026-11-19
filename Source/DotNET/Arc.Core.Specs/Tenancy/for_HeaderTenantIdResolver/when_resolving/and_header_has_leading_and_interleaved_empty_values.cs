// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_HeaderTenantIdResolver.when_resolving;

public class and_header_has_leading_and_interleaved_empty_values : given.a_header_tenant_id_resolver
{
    string _result;

    void Establish() => _headers[Constants.DefaultTenantIdHeader] = ", acme,, ,acme,";
    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_nonempty_tenant() => _result.ShouldEqual("acme");
}
