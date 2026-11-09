// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_an_oauth_caller_lacks_the_write_scope : an_mcp_host
{
    McpResponse _refused;
    int _dispatchedWhenRefused;
    McpResponse _control;

    protected override McpProfile Profile => McpProfile.OAuthOnly;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.OAuthGrant, ReadScope);
        _refused = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenRefused = _probe.Count;

        Authenticate(CallerCredentialKind.OAuthGrant, ReadScope, WriteScope);
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_as_forbidden() => _refused.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact]
    void should_challenge_for_step_up_through_the_configured_metadata() =>
        _refused.Challenge.ShouldEqual($"Bearer error=\"insufficient_scope\", scope=\"app:write\", resource_metadata=\"{PublicOrigin}/.well-known/oauth-protected-resource/mcp\"");
    [Fact] void should_not_dispatch_the_refused_call() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_the_call_once_the_scope_is_granted_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
