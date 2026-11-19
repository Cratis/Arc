// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_resolving_tenant_header;

public class with_conflicting_repeated_values : given.a_header_tenant_resolver
{
    string _result;

    Task Establish() => SendTenantHeaders("acme", "evil");
    void Because() => _result = _resolver.Resolve();

    /// <summary>
    /// Known host limitation, not fail-closed coverage: managed HttpListener drops earlier field lines.
    /// Windows HTTP.sys combines them, allowing Arc to detect the conflict.
    /// </summary>
    [Fact] void should_pin_the_hosts_duplicate_header_limitation() => _result.ShouldEqual(OperatingSystem.IsWindows() ? string.Empty : "evil");
}
