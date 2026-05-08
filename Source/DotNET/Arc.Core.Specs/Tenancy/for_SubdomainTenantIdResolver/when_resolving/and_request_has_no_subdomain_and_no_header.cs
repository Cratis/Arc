// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_request_has_no_subdomain_and_no_header : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns("localhost");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_empty_string() => _result.ShouldEqual(string.Empty);
}
