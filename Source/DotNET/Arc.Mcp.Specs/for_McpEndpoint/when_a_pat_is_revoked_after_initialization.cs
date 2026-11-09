// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_pat_is_revoked_after_initialization : a_pat_authenticated_host
{
    McpResponse _before;
    McpResponse _after;

    async Task Because()
    {
        await Initialize();
        _before = await SessionCall(_pat, withSession: false);
        _record = _record with { Revoked = true };
        _after = await SessionCall(_pat);
    }

    [Fact] void should_initialize_with_the_intended_mcp_resource_credential() => _initialized.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_accept_the_active_pat() => (_before.Status, _before.IsToolError).ShouldEqual((HttpStatusCode.OK, false));
    [Fact] void should_refuse_on_the_next_request_without_a_positive_cache_grace_period() => _after.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_read_authoritative_revocation_for_every_request() => _authorityReads.ShouldEqual(3);
    [Fact] void should_not_dispatch_after_revocation() => _probe.Count.ShouldEqual(1);
}
