// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantHeaderPreservation;

public class when_strict_non_header_tenancy_starts_on_http_listener : given.an_http_listener_tenant_host
{
    void Establish() => _options.Tenancy.RequireDuplicateHeaderPreservation = true;
    void Because() => StartWith(new FixedTenantIdResolver(Options.Create(_options)));

    [Fact] void should_not_refuse_startup() => _error.ShouldBeNull();
    [Fact] void should_not_warn_about_unused_headers() => Warnings.ShouldEqual(0);
}
