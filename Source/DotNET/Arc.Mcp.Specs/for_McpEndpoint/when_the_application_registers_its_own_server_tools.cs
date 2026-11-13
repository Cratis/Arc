// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_application_registers_its_own_server_tools : an_mcp_host
{
    McpResponse _listed;
    McpResponse _called;
    McpResponse _catalogCall;

    protected override void ConfigureOptions(McpOptions options) => options.ExclusiveTools = false;

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddMcpServer().WithTools([McpServerTool.Create((string text) => $"echo:{text}", new McpServerToolCreateOptions { Name = "app_echo", Description = "Echoes." })]);

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _listed = await ListTools();
        _called = await CallTool("app_echo", """{"text":"hi"}""");
        _catalogCall = await CallTool("who_am_i", "{}");
    }

    [Fact] void should_list_the_server_tool_next_to_the_catalog_tools_the_caller_can_call() => _listed.ToolNames.ShouldContainOnly("app_echo", "who_am_i");
    [Fact] void should_call_the_server_tool() => _called.ToolText.ShouldEqual("echo:hi");
    [Fact] void should_keep_serving_the_catalog_tools() => _catalogCall.IsToolError.ShouldBeFalse();
}
