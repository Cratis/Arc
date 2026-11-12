// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_the_catalog_is_built : a_catalog_context
{
    Exception? _mutateTools;
    Exception? _mutateScopes;

    void Establish() => _exposure.Expose<DeclaredCommand>("declared_command", "A declared command.");

    void Because()
    {
        Build();
        _exposure.Expose<UndeclaredCommand>("late_command", "Declared after the catalog was built.");
        _options.WriteScope = "changed:write";
        _mutateTools = Catch.Exception(() => ((ICollection<McpToolDescriptor>)_catalog.Tools).Add(_catalog.Tools.First()));
        _mutateScopes = Catch.Exception(() => ((IList<string>)_catalog.Tools.First().RequiredScopes).Add("extra:scope"));
    }

    [Fact] void should_hold_the_tool_declared_before_it_was_built() => _catalog.TryGet("declared_command", out _).ShouldBeTrue();
    [Fact] void should_not_gain_a_tool_declared_afterwards() => _catalog.TryGet("late_command", out _).ShouldBeFalse();
    [Fact] void should_not_change_when_options_change() => _catalog.Tools.First().RequiredScopes.ShouldContainOnly("app:write");
    [Fact] void should_refuse_to_add_a_tool() => _mutateTools.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_refuse_to_add_a_scope() => _mutateScopes.ShouldBeOfExactType<NotSupportedException>();
}
