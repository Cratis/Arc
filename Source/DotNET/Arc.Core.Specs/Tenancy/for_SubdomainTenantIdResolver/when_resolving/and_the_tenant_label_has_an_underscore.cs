// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// An underscore is not legal in a DNS label, but it is ASCII, so it takes the fast path past the punycode
/// conversion and reaches the base domain match unexamined.
/// </summary>
public class and_the_tenant_label_has_an_underscore : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"a_b.{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_label_as_a_tenant() => _result.ShouldNotEqual("a_b");
}
