// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_command_omits_a_required_argument : an_mcp_host
{
    McpResponse _response;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _response = await CallTool("write_note", "{}");
    }

    [Fact] void should_return_a_tool_error() => _response.IsToolError.ShouldBeTrue();
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
