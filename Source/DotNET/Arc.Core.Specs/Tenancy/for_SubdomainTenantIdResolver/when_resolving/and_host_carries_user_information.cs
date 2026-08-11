// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// User information in front of the host is not part of the host, but a request can put it there anyway. It reaches
/// the base domain match as one label, so only the letter-digit-hyphen rule stops it.
/// </summary>
public class and_host_carries_user_information : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"user@acme.{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_label_as_a_tenant() => _result.ShouldNotEqual("user@acme");
}
