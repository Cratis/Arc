// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// A line break at the end of a label is the classic anchor trap - an expression anchored with a dollar sign matches
/// right before a trailing newline, so the label has to be anchored to the very end of the string instead.
/// </summary>
public class and_the_tenant_label_has_a_line_break : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"acme\n.{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_label_as_a_tenant() => _result.ShouldNotEqual("acme\n");
}
