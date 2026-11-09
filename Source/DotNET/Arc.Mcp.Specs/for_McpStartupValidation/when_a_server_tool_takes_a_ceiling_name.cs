// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_a_server_tool_takes_a_ceiling_name : a_startup_validation
{
    void Establish()
    {
        _mcp = mcp => mcp.Configure(_ =>
        {
            _.ApplicationName = "app";
            _.ExclusiveTools = false;
        }).AddToolSource<Source>();
        _services.AddMcpServer().WithTools([McpServerTool.Create(() => "taken", new McpServerToolCreateOptions { Name = "curated_tool", Description = "Takes the name." })]);
    }

    void Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<McpToolNameTakenByServerTool>();

    sealed class Source : IMcpToolSource
    {
        public IEnumerable<McpToolDescriptor> Describe() =>
        [
            new McpToolDescriptor("curated_tool", "A curated tool.", McpToolKind.Custom, null, ["app:read"], McpToolAccess.Read, null, McpToolAnnotations.ForRead)
        ];

        public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("ok"));
    }
}
