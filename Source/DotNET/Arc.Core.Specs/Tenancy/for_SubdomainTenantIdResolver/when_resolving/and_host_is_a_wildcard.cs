// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// A wildcard host is a single label in front of the base domain, so only the letter-digit-hyphen rule stops it from
/// becoming a tenant ID that is concatenated into a database name.
/// </summary>
public class and_host_is_a_wildcard : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"*.{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_label_as_a_tenant() => _result.ShouldNotEqual("*");
}
