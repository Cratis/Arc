// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Mcp.for_McpServerToolsConfiguration.given;
using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp.for_McpServerToolsConfiguration;

public class when_a_server_tool_takes_a_ceiling_name : a_ceiling_context
{
    Exception? _sameName;
    Exception? _otherCase;
    McpServerOptions _merged;
    McpServerOptions _exclusive;
    McpServerOptions _default;
    WarningRecorder _logger;
    WarningRecorder _defaultLogger;
    WarningRecorder _emptyLogger;
    WarningRecorder _missingLogger;

    void Because()
    {
        _emptyLogger = new();
        Configure(false, null, _emptyLogger);
        _missingLogger = new();
        Configure(false, null, _missingLogger, missingCollection: true);
        _defaultLogger = new();
        _default = Configure(null, "app_echo", _defaultLogger);
        _logger = new();
        Configure(false, "app_echo", _logger);
        _sameName = Catch.Exception(() => Configure(false, "declared_command"));
        _otherCase = Catch.Exception(() => Configure(false, "Declared_Command"));
        _merged = Configure(false, "app_echo");
        _exclusive = Configure(true, "declared_command");
    }

    [Fact] void should_fail_when_a_server_tool_has_a_ceiling_name() => _sameName.ShouldBeOfExactType<McpToolNameTakenByServerTool>();
    [Fact] void should_fail_when_the_names_differ_only_in_case() => _otherCase.ShouldBeOfExactType<McpToolNameTakenByServerTool>();
    [Fact] void should_serve_the_server_tool_next_to_the_ceiling() => _merged.ToolCollection!.PrimitiveNames.ShouldContainOnly("app_echo", "declared_command");
    [Fact] void should_serve_only_the_ceiling_when_exclusive() => _exclusive.ToolCollection!.PrimitiveNames.ShouldContainOnly("declared_command");
    [Fact] void should_remove_custom_call_handlers_when_exclusive() => _exclusive.Handlers.CallToolHandler.ShouldBeNull();
    [Fact] void should_serve_only_the_ceiling_by_default() => _default.ToolCollection!.PrimitiveNames.ShouldContainOnly("declared_command");
    [Fact] void should_remove_custom_call_handlers_by_default() => _default.Handlers.CallToolHandler.ShouldBeNull();
    [Fact] void should_not_warn_by_default() => _defaultLogger.Warnings.ShouldEqual(0);
    [Fact] void should_warn_when_serving_server_tools() => _logger.Warnings.ShouldEqual(1);
    [Fact] void should_not_warn_with_an_empty_server_tool_collection() => _emptyLogger.Warnings.ShouldEqual(0);
    [Fact] void should_not_warn_without_a_server_tool_collection() => _missingLogger.Warnings.ShouldEqual(0);

    McpServerOptions Configure(bool? exclusive, string? serverToolName, ILogger<McpServerToolsConfiguration>? logger = null, bool missingCollection = false)
    {
        var mcp = new McpOptions { ApplicationName = "app" };
        if (exclusive is { } value)
        {
            mcp.ExclusiveTools = value;
        }
        var options = Options.Create(mcp);
        var factory = new McpToolCeilingFactory(new McpExposure().Expose<DeclaredCommand>("declared_command", "One."), options, _commands, _queries, []);
        var operations = new McpOperations(Substitute.For<ICommandPipeline>(), Substitute.For<IQueryPipeline>(), _queries, Options.Create(new ArcOptions()));
        var dispatcher = new McpToolDispatcher(Substitute.For<IHttpContextAccessor>(), factory, operations, NullLogger<McpToolDispatcher>.Instance);
        var server = new McpServerOptions
        {
            ToolCollection = missingCollection ? null : serverToolName is null ? [] :
                [McpServerTool.Create(() => "echo", new McpServerToolCreateOptions { Name = serverToolName, Description = "Echoes." })]
        };
        server.Handlers.CallToolHandler = (_, _) => ValueTask.FromResult(new ModelContextProtocol.Protocol.CallToolResult());

        new McpServerToolsConfiguration(factory, dispatcher, options, logger ?? NullLogger<McpServerToolsConfiguration>.Instance).PostConfigure(null, server);
        return server;
    }
}
