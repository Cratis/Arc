// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantHeaderPreservation;

public class when_strict_subdomain_tenancy_starts_on_http_listener : given.an_http_listener_tenant_host
{
    void Establish()
    {
        _options.Tenancy.RequireDuplicateHeaderPreservation = true;
        _options.Tenancy.BaseDomain = "example.test";
        _options.Tenancy.IngressValidatesTenant = true;
    }

    void Because() => StartWith(new SubdomainTenantIdResolver(_accessor, Options.Create(_options)));

    [Fact] void should_refuse_the_header_fallback_despite_ingress_membership_validation() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
