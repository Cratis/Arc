// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_the_application_has_several_write_scopes : a_catalog_context
{
    Exception? _otherWriteScope;
    Exception? _defaultWriteScope;
    Exception? _readAndWriteScope;
    Exception? _noWriteScope;
    Exception? _readScopeOnly;
    Exception? _alternativeWithoutWriteScope;
    Exception? _alternativeWithWriteScope;
    Exception? _readTool;

    void Establish()
    {
        _options.WriteScopes.Add("notes:publish");
        _options.WriteScopes.Add("archive:write");
    }

    void Because()
    {
        _otherWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["notes:publish"]));
        _defaultWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["app:write"]));
        _readAndWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["app:read", "archive:write"]));
        _noWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["audit:read"]));
        _readScopeOnly = Catch.Exception(() => Catalog(McpToolAccess.Write, ["app:read"]));
        _alternativeWithoutWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["notes:publish"], ["app:read"]));
        _alternativeWithWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["notes:publish"], ["audit:read", "archive:write"]));
        _readTool = Catch.Exception(() => Catalog(McpToolAccess.Read, ["app:read"]));
    }

    [Fact] void should_accept_a_write_tool_requiring_another_write_scope() => _otherWriteScope.ShouldBeNull();
    [Fact] void should_accept_a_write_tool_requiring_the_write_scope() => _defaultWriteScope.ShouldBeNull();
    [Fact] void should_accept_a_write_tool_requiring_a_read_and_a_write_scope() => _readAndWriteScope.ShouldBeNull();
    [Fact] void should_refuse_a_write_tool_requiring_no_write_scope() => _noWriteScope.ShouldBeOfExactType<McpWriteToolLacksWriteScope>();
    [Fact] void should_refuse_a_write_tool_requiring_only_the_read_scope() => _readScopeOnly.ShouldBeOfExactType<McpWriteToolLacksWriteScope>();
    [Fact] void should_refuse_a_write_alternative_without_a_write_scope() => _alternativeWithoutWriteScope.ShouldBeOfExactType<McpWriteToolLacksWriteScope>();
    [Fact] void should_accept_a_write_alternative_with_a_write_scope() => _alternativeWithWriteScope.ShouldBeNull();
    [Fact] void should_accept_a_read_tool_requiring_the_read_scope() => _readTool.ShouldBeNull();

    McpToolCatalog Catalog(McpToolAccess access, string[] scopes, params string[][] alternatives) =>
        McpToolCatalog.Build(_exposure, _options, _commands, _queries, [new Source(access, scopes, alternatives)]);

    sealed class Source(McpToolAccess access, string[] scopes, string[][] alternatives) : IMcpToolSource
    {
        public IEnumerable<McpToolDescriptor> Describe() =>
        [
            McpToolDescriptor.Custom("curated_tool", "A curated tool.", scopes, access) with
            {
                AlternativeScopes = [.. alternatives]
            }
        ];

        public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("ok"));
    }
}
