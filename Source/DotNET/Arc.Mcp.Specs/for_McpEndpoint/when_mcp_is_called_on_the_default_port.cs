// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_mcp_is_called_on_the_default_port : an_mcp_host
{
    McpResponse _onDefaultPort;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _onDefaultPort = await Post(
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"write_note","arguments":{"text":"x"}}}""",
            port: _defaultPort);
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_not_serve_the_endpoint_on_the_default_port() => _onDefaultPort.Status.ShouldNotEqual(HttpStatusCode.OK);
    [Fact] void should_not_dispatch_the_refused_call() => _probe.Count.ShouldEqual(1);
    [Fact] void should_serve_the_endpoint_on_the_capability_port() => _control.IsToolError.ShouldBeFalse();
}
