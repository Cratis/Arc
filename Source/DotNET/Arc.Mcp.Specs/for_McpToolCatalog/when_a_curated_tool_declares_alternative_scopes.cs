// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_a_curated_tool_declares_alternative_scopes : a_catalog_context
{
    Exception? _emptySet;
    Exception? _blankScope;
    Exception? _writeWithoutWriteScope;
    Exception? _writeWithWriteScope;
    McpToolCatalog _valid;

    void Because()
    {
        _emptySet = Catch.Exception(() => Catalog(McpToolAccess.Read, []));
        _blankScope = Catch.Exception(() => Catalog(McpToolAccess.Read, ["audit:read", " "]));
        _writeWithoutWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["admin"]));
        _writeWithWriteScope = Catch.Exception(() => Catalog(McpToolAccess.Write, ["admin", "app:write"]));
        _valid = Catalog(McpToolAccess.Read, ["audit:read", "audit:export"]);
    }

    [Fact] void should_refuse_an_empty_alternative() => _emptySet.ShouldBeOfExactType<McpToolHasNoScope>();
    [Fact] void should_refuse_a_blank_alternative_scope() => _blankScope.ShouldBeOfExactType<McpToolScopeBlank>();
    [Fact] void should_refuse_a_write_alternative_without_the_write_scope() => _writeWithoutWriteScope.ShouldBeOfExactType<McpWriteToolLacksWriteScope>();
    [Fact] void should_accept_a_write_alternative_with_the_write_scope() => _writeWithWriteScope.ShouldBeNull();
    [Fact] void should_keep_the_alternative() => _valid.Tools.Single().AlternativeScopes.Single().ShouldContainOnly("audit:read", "audit:export");
    [Fact] void should_freeze_the_alternatives() => Catch.Exception(() => ((IList<string>)_valid.Tools.Single().AlternativeScopes[0]).Add("extra")).ShouldNotBeNull();

    McpToolCatalog Catalog(McpToolAccess access, string[] alternative) =>
        McpToolCatalog.Build(_exposure, _options, _commands, _queries, [new Source(access, alternative)]);

    sealed class Source(McpToolAccess access, string[] alternative) : IMcpToolSource
    {
        public IEnumerable<McpToolDescriptor> Describe() =>
        [
            McpToolDescriptor.Custom("curated_tool", "A curated tool.", [access == McpToolAccess.Write ? "app:write" : "app:read"], access) with
            {
                AlternativeScopes = [alternative]
            }
        ];

        public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("ok"));
    }
}
