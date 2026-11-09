// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_no_scope_is_configured : a_ceiling_context
{
    Exception? _control;

    void Establish()
    {
        _options = new McpOptions();
        _exposure.Expose<DeclaredCommand>("declared_command", "A declared command.");
    }

    void Because()
    {
        Build();
        _control = Catch.Exception(() => McpToolCeiling.Build(_exposure, new McpOptions { WriteScope = "explicit:write" }, _commands, _queries));
    }

    [Fact] void should_refuse_a_tool_that_would_require_no_scope() => _error.ShouldBeOfExactType<McpToolHasNoScope>();
    [Fact] void should_accept_an_explicit_scope() => _control.ShouldBeNull();
}
