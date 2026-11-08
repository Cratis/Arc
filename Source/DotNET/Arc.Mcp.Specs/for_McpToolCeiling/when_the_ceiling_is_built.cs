// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_the_ceiling_is_built : a_ceiling_context
{
    Exception? _mutateTools;
    Exception? _mutateScopes;

    void Establish() => _exposure.Expose<DeclaredCommand>("declared_command", "A declared command.");

    void Because()
    {
        Build();
        _exposure.Expose<UndeclaredCommand>("late_command", "Declared after the ceiling was built.");
        _options.WriteScope = "changed:write";
        _mutateTools = Catch.Exception(() => ((ICollection<McpToolDescriptor>)_ceiling.Tools).Add(_ceiling.Tools.First()));
        _mutateScopes = Catch.Exception(() => ((IList<string>)_ceiling.Tools.First().RequiredScopes).Add("extra:scope"));
    }

    [Fact] void should_hold_the_tool_declared_before_it_was_built() => _ceiling.TryGet("declared_command", out _).ShouldBeTrue();
    [Fact] void should_not_gain_a_tool_declared_afterwards() => _ceiling.TryGet("late_command", out _).ShouldBeFalse();
    [Fact] void should_not_change_when_options_change() => _ceiling.Tools.First().RequiredScopes.ShouldContainOnly("app:write");
    [Fact] void should_refuse_to_add_a_tool() => _mutateTools.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_refuse_to_add_a_scope() => _mutateScopes.ShouldBeOfExactType<NotSupportedException>();
}
