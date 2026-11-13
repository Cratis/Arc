// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantHeaderPreservation;

public class when_default_header_tenancy_starts_on_http_listener : given.an_http_listener_tenant_host
{
    void Because() => StartWith(new HeaderTenantIdResolver(_accessor, Options.Create(_options)));

    [Fact] void should_leave_strict_preservation_disabled() => _options.Tenancy.RequireDuplicateHeaderPreservation.ShouldBeFalse();
    [Fact] void should_start_without_refusal() => _error.ShouldBeNull();
    [Fact] void should_warn_once() => Warnings.ShouldEqual(1);
    [Fact] void should_warn_about_stripping_all_occurrences() => WarningMessage().ShouldContain("strip ALL incoming tenant-header occurrences and inject exactly one value");
    [Fact] void should_warn_about_direct_access() => WarningMessage().ShouldContain("prevent direct backend access");

    string WarningMessage() => _warnings.Single();
}
