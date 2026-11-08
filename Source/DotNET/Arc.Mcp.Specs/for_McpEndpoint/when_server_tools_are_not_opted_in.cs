// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_server_tools_are_not_opted_in : an_mcp_host
{
    McpResponse _listed;
    McpResponse _called;

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddMcpServer().WithTools([McpServerTool.Create((string text) => $"echo:{text}", new McpServerToolCreateOptions { Name = "app_echo", Description = "Echoes." })]);

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _listed = await ListTools();
        _called = await CallTool("app_echo", """{"text":"hi"}""");
    }

    [Fact] void should_list_only_the_ceiling_tools() => _listed.ToolNames.ShouldContainOnly("who_am_i");
    [Fact] void should_not_call_the_server_tool() => _called.IsProtocolError.ShouldBeTrue();
}
