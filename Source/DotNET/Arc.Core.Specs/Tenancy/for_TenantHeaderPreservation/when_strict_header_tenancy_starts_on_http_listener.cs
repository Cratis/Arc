// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantHeaderPreservation;

public class when_strict_header_tenancy_starts_on_http_listener : given.an_http_listener_tenant_host
{
    void Establish() => _options.Tenancy.RequireDuplicateHeaderPreservation = true;
    void Because() => StartWith(new HeaderTenantIdResolver(_accessor, Options.Create(_options)));

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_explain_the_host_limitation() => _error.Message.ShouldContain("HttpListener cannot guarantee preservation");
    [Fact] void should_name_the_option() => _error.Message.ShouldContain("RequireDuplicateHeaderPreservation");
    [Fact] void should_require_ingress_sanitization() => _error.Message.ShouldContain("strips all incoming tenant-header occurrences and injects exactly one value");
    [Fact] void should_require_backend_isolation() => _error.Message.ShouldContain("direct backend access prevented");
}
