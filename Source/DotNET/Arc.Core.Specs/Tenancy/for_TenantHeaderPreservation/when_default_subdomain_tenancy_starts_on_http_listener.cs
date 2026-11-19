// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantHeaderPreservation;

public class when_default_subdomain_tenancy_starts_on_http_listener : given.an_http_listener_tenant_host
{
    void Establish() => _options.Tenancy.BaseDomain = "example.test";
    void Because() => StartWith(new SubdomainTenantIdResolver(_accessor, Options.Create(_options)));

    [Fact] void should_start_without_refusal() => _error.ShouldBeNull();
    [Fact] void should_warn_about_header_fallback_preservation() => Warnings.ShouldEqual(1);
}
