// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_pat_fences_change_after_initialization : a_pat_authenticated_host
{
    McpResponse _scopeRefusal;
    McpResponse _tenantRefusal;

    async Task Because()
    {
        await Initialize();
        var original = _record;
        _record = original with { Scopes = new HashSet<string> { ReadScope } };
        _scopeRefusal = await SessionCall(_pat);
        _record = original with { Tenant = new TenantId("tenant-b") };
        _tenantRefusal = await SessionCall(_pat);
    }

    [Fact] void should_initialize() => _initialized.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_recheck_the_write_scope() => _scopeRefusal.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_recheck_the_tenant() => _tenantRefusal.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
