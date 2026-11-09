// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_pat_membership_is_withdrawn_after_initialization : a_pat_authenticated_host
{
    McpResponse _after;

    async Task Because()
    {
        await Initialize();
        _policyAllows = false;
        _after = await SessionCall(_pat);
    }

    [Fact] void should_initialize() => _initialized.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_recheck_current_membership_policy() => _policyChecks.ShouldEqual(2);
    [Fact] void should_refuse_despite_the_session_id() => _after.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
