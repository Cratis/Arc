// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_tool_accepts_alternative_scopes : an_mcp_host
{
    McpResponse _listedForAlternative;
    McpResponse _calledWithAlternative;
    McpResponse _listedForPartial;
    McpResponse _calledWithPartial;
    McpResponse _calledWithRequired;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.AddToolSource<AlternativeScopeTool>();

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, "audit:read", "audit:export");
        _listedForAlternative = await ListTools();
        _calledWithAlternative = await CallTool("audit_log", "{}");

        Authenticate(CallerCredentialKind.PersonalAccessToken, "audit:read");
        _listedForPartial = await ListTools();
        _calledWithPartial = await CallTool("audit_log", "{}");

        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _calledWithRequired = await CallTool("audit_log", "{}");
    }

    [Fact] void should_list_the_tool_for_a_complete_alternative() => _listedForAlternative.ToolNames.ShouldContainOnly("audit_log");
    [Fact] void should_call_the_tool_with_a_complete_alternative() => _calledWithAlternative.ToolText.ShouldEqual("audited");
    [Fact] void should_not_list_the_tool_for_a_partial_alternative() => _listedForPartial.ToolNames.ShouldBeEmpty();
    [Fact] void should_refuse_a_partial_alternative_as_forbidden() => _calledWithPartial.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_challenge_for_the_required_scopes() => _calledWithPartial.Challenge.ShouldEqual("Bearer error=\"insufficient_scope\", scope=\"app:read\"");
    [Fact] void should_call_the_tool_with_the_required_scopes() => _calledWithRequired.ToolText.ShouldEqual("audited");
}
