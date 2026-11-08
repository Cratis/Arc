// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_curated_tool_reads_the_protocol_request : an_mcp_host
{
    McpResponse _called;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.AddToolSource<RequestContextTool>();

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _called = await CallTool("request_context", "{}");
    }

    [Fact] void should_hand_the_protocol_request_and_server_to_the_tool() => _called.ToolText.ShouldEqual("request_context|True");
}
