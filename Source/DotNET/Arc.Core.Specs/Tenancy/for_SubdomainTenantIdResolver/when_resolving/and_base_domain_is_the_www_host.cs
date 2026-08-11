// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// An application served from <c>www.myapp.com</c> configures that as its base domain, and its own host then carries
/// no tenant - <c>www</c> is part of the base domain here, not a label in front of it. No host name is special to the
/// resolver; which one is the application's own is what the base domain says. This is the same shape as
/// <c>and_host_is_the_base_domain</c> written with a three label base domain, so read it as documentation of the
/// <c>www</c> case; <c>and_host_is_a_tenant_below_the_www_base_domain</c> is the half that carries the evidence.
/// </summary>
public class and_base_domain_is_the_www_host : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        ConfigureBaseDomain($"www.{BaseDomain}");
        _context.Host.Returns($"www.{BaseDomain}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
}
