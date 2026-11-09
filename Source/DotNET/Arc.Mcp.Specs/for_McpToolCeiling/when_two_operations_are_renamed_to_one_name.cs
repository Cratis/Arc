// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_two_operations_are_renamed_to_one_name : a_ceiling_context
{
    Exception? _commandAndQuery;
    Exception? _queryAndAttributedQuery;
    McpToolCeiling? _control;

    void Establish() => AddPerformerFor(nameof(ReadModelWithQueries.Exposed));

    void Because()
    {
        _commandAndQuery = Catch.Exception(() => McpToolCeiling.Build(
            new McpExposure().Expose<DeclaredCommand>("shared_name", "A command.").ExposeQuery(QueryName(nameof(ReadModelWithQueries.Everything)), "shared_name", "A query."),
            _options,
            _commands,
            _queries));
        _queryAndAttributedQuery = Catch.Exception(() => McpToolCeiling.Build(
            new McpExposure().ExposeQuery(QueryName(nameof(ReadModelWithQueries.Everything)), "attributed_query", "Renamed onto an attribute name."),
            _options,
            _commands,
            _queries));
        _control = McpToolCeiling.Build(
            new McpExposure().Expose<DeclaredCommand>("a_name", "A command.").ExposeQuery(QueryName(nameof(ReadModelWithQueries.Everything)), "another_name", "A query."),
            _options,
            _commands,
            _queries);
    }

    [Fact] void should_refuse_a_command_and_a_query_with_one_name() => _commandAndQuery.ShouldBeOfExactType<McpToolNameDuplicated>();
    [Fact] void should_refuse_a_rename_onto_an_attribute_name() => _queryAndAttributedQuery.ShouldBeOfExactType<McpToolNameDuplicated>();
    [Fact] void should_hold_both_when_the_names_differ() => _control!.Tools.Count.ShouldEqual(3);
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
