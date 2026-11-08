// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_operations_are_exposed : a_ceiling_context
{
    McpToolDescriptor? _command;
    McpToolDescriptor? _query;
    McpToolDescriptor? _attributedCommand;
    McpToolDescriptor? _attributedQuery;

    void Establish()
    {
        AddHandlerFor<AttributedCommand>();
        AddPerformerFor(nameof(ReadModelWithQueries.Exposed));
        _exposure
        .Expose<DeclaredCommand>("declared_command", "A declared command.")
        .ExposeQuery(QueryName(nameof(ReadModelWithQueries.Everything)), "everything", "All of it.");
    }

    void Because()
    {
        Build();
        _ceiling.TryGet("declared_command", out _command);
        _ceiling.TryGet("everything", out _query);
        _ceiling.TryGet("attributed_command", out _attributedCommand);
        _ceiling.TryGet("attributed_query", out _attributedQuery);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_hold_the_declared_command_as_a_write_tool() => (_command!.Kind, _command.Access, _command.Operation).ShouldEqual((McpToolKind.Command, McpToolAccess.Write, typeof(DeclaredCommand)));
    [Fact] void should_require_the_write_scope_for_the_command() => _command!.RequiredScopes.ShouldContainOnly("app:write");
    [Fact] void should_hold_the_declared_query_as_a_read_tool() => (_query!.Kind, _query.Access, _query.Operation).ShouldEqual((McpToolKind.Query, McpToolAccess.Read, new FullyQualifiedQueryName(QueryName(nameof(ReadModelWithQueries.Everything)))));
    [Fact] void should_require_the_read_scope_for_the_query() => _query!.RequiredScopes.ShouldContainOnly("app:read");
    [Fact] void should_hold_a_command_declared_by_attribute() => _attributedCommand.ShouldNotBeNull();
    [Fact] void should_hold_a_query_declared_by_attribute() => _attributedQuery.ShouldNotBeNull();
    [Fact] void should_hold_nothing_else() => _ceiling.Tools.Count.ShouldEqual(4);
    [Fact] void should_not_reach_an_undeclared_command() => _ceiling.TryGet("undeclared_command", out _).ShouldBeFalse();
    [Fact] void should_not_reach_an_undeclared_query() => _ceiling.TryGet("not_exposed", out _).ShouldBeFalse();
    [Fact] void should_not_reach_a_name_with_different_casing() => _ceiling.TryGet("Declared_Command", out _).ShouldBeFalse();
    [Fact] void should_not_reach_a_name_with_a_path() => _ceiling.TryGet("../declared_command", out _).ShouldBeFalse();
    [Fact] void should_not_reach_a_name_with_trailing_space() => _ceiling.TryGet("declared_command ", out _).ShouldBeFalse();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
