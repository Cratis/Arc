// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_an_unknown_operation_is_exposed : a_catalog_context
{
    Exception? _command;
    Exception? _query;
    Exception? _control;

    void Because()
    {
        _command = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<UnknownCommand>("unknown_command", "Unknown."), _options, _commands, _queries));
#pragma warning disable CS0618 // The obsolete string overload stays covered until it is removed in Arc 24.
        _query = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().ExposeQuery(QueryName("Missing"), "unknown_query", "Unknown."), _options, _commands, _queries));
#pragma warning restore CS0618
        _control = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DeclaredCommand>("declared_command", "Known."), _options, _commands, _queries));
    }

    [Fact] void should_refuse_an_unknown_command() => _command.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_refuse_an_unknown_query() => _query.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_accept_a_known_command() => _control.ShouldBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
