// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// U+3002 is one of the label separators IDNA treats as a full stop, so this is the same domain as
/// <c>acme.myapp.com</c> and resolves the same tenant. Pinned rather than rejected for the same reason as the
/// fullwidth spelling - it is what the standard says and what browsers do.
/// </summary>
public class and_host_uses_an_ideographic_label_separator : given.a_subdomain_tenant_id_resolver
{
    const string IdeographicFullStop = "\u3002";

    string _result;

    void Establish() => _context.Host.Returns($"acme{IdeographicFullStop}{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_subdomain_as_the_tenant_id() => _result.ShouldEqual("acme");
}
