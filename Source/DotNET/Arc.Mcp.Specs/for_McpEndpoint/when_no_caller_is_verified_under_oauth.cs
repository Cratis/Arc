// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_no_caller_is_verified_under_oauth : an_mcp_host
{
    McpResponse _call;
    int _dispatchedWhenRefused;
    McpResponse _control;

    protected override McpProfile Profile => McpProfile.OAuthOnly;

    async Task Because()
    {
        Unauthenticate();
        _call = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenRefused = _probe.Count;

        Authenticate(CallerCredentialKind.OAuthGrant, ReadScope, WriteScope);
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_as_unauthorized() => _call.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_point_to_the_configured_metadata() => _call.Challenge.ShouldEqual($"Bearer resource_metadata=\"{PublicOrigin}/.well-known/oauth-protected-resource/mcp\"");
    [Fact] void should_not_dispatch_the_refused_call() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_the_call_for_a_verified_caller_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
