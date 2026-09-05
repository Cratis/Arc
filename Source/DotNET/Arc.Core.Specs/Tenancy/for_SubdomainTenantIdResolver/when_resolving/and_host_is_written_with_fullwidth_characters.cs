// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// Fullwidth Latin letters map to their ASCII counterparts, so a host written with them is the same domain and must
/// resolve the same tenant. This is IDNA compatibility mapping working as specified - browsers do the same - and it
/// is pinned rather than rejected, because rejecting it would put Arc at odds with the standard. It does mean a WAF
/// or ingress matching the literal host string sees a different value than Arc does.
/// </summary>
public class and_host_is_written_with_fullwidth_characters : given.a_subdomain_tenant_id_resolver
{
    const string FullwidthSmallLetterA = "\uFF41";

    string _result;

    void Establish() => _context.Host.Returns($"{FullwidthSmallLetterA}cme.{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_ascii_label_as_the_tenant_id() => _result.ShouldEqual("acme");
}
