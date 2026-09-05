// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// IDNA maps ignorable characters - zero width space, soft hyphen, byte order mark, word joiner - away, so a label
/// carrying one is the same label without it and resolves the same tenant. Anything upstream that matches the literal
/// host string sees a value Arc does not, which is why the equivalence class is written down in the documentation
/// rather than left for a reader to discover.
/// </summary>
public class and_host_has_a_zero_width_space_in_the_label : given.a_subdomain_tenant_id_resolver
{
    const string ZeroWidthSpace = "\u200B";

    string _result;

    void Establish() => _context.Host.Returns($"ac{ZeroWidthSpace}me.{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_label_without_it_as_the_tenant_id() => _result.ShouldEqual("acme");
}
