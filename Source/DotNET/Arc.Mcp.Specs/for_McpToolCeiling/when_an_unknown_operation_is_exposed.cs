// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_an_unknown_operation_is_exposed : a_ceiling_context
{
    Exception? _command;
    Exception? _query;
    Exception? _control;

    void Because()
    {
        _command = Catch.Exception(() => McpToolCeiling.Build(new McpExposure().Expose<UnknownCommand>("unknown_command", "Unknown."), _options, _commands, _queries));
        _query = Catch.Exception(() => McpToolCeiling.Build(new McpExposure().ExposeQuery(QueryName("Missing"), "unknown_query", "Unknown."), _options, _commands, _queries));
        _control = Catch.Exception(() => McpToolCeiling.Build(new McpExposure().Expose<DeclaredCommand>("declared_command", "Known."), _options, _commands, _queries));
    }

    [Fact] void should_refuse_an_unknown_command() => _command.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_refuse_an_unknown_query() => _query.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_accept_a_known_command() => _control.ShouldBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
