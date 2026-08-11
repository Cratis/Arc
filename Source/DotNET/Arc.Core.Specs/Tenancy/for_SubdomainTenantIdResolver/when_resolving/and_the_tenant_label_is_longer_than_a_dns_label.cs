// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// An over-long non-ASCII label was already rejected, because the punycode conversion refuses it, while an ASCII one
/// of any length went straight through into the tenant ID and from there into a database name. The length bound on
/// the label closes that asymmetry.
/// </summary>
public class and_the_tenant_label_is_longer_than_a_dns_label : given.a_subdomain_tenant_id_resolver
{
    const int LabelLength = 300;

    string _label;
    string _result;

    void Establish()
    {
        _label = new string('a', LabelLength);
        _context.Host.Returns($"{_label}.{BaseDomain}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_label_as_a_tenant() => _result.ShouldNotEqual(_label);
}
