// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_a_curated_write_tool_does_not_require_the_write_scope : a_catalog_context
{
    Exception? _readScopeOnly;
    Exception? _withWriteScope;
    Exception? _readTool;

    void Establish() => _options = new McpOptions { ReadScope = "explicit:read", WriteScope = "explicit:write" };

    void Because()
    {
        _readScopeOnly = Catch.Exception(() => Catalog(McpToolAccess.Write, "explicit:read"));
        _withWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, "explicit:read", "explicit:write"));
        _readTool = Catch.Exception(() => Catalog(McpToolAccess.Read, "explicit:read"));
    }

    [Fact] void should_refuse_a_write_tool_requiring_only_the_read_scope() => _readScopeOnly.ShouldBeOfExactType<McpWriteToolLacksWriteScope>();
    [Fact] void should_accept_a_write_tool_requiring_the_write_scope() => _withWriteScope.ShouldBeNull();
    [Fact] void should_accept_a_read_tool_requiring_the_read_scope() => _readTool.ShouldBeNull();

    McpToolCatalog Catalog(McpToolAccess access, params string[] scopes) =>
        McpToolCatalog.Build(_exposure, _options, _commands, _queries, [new Source(access, scopes)]);

    sealed class Source(McpToolAccess access, string[] scopes) : IMcpToolSource
    {
        public IEnumerable<McpToolDescriptor> Describe() =>
        [
            new McpToolDescriptor("curated_tool", "A curated tool.", McpToolKind.Custom, null, scopes, access, """{"type":"object"}""", McpToolAnnotations.ForRead)
        ];

        public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("ok"));
    }
}
