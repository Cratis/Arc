// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Mcp.for_McpToolMapper.given;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_the_operation_cannot_be_mapped : a_mapper_context
{
    Exception? _custom;
    Exception? _unknownCommand;
    Exception? _unknownQuery;
    Exception? _control;

    void Because()
    {
        _custom = Catch.Exception(() => Map(new McpExposureDeclaration(McpToolKind.Custom, typeof(DeclaredCommand), "custom", "Custom.")));
        _unknownCommand = Catch.Exception(() => MapCommand<UnknownCommand>("unknown_command"));
        _unknownQuery = Catch.Exception(() => MapQuery("Not.A.Query", "unknown_query"));
        _control = Catch.Exception(() => MapCommand<DeclaredCommand>("control"));
    }

    [Fact] void should_refuse_a_custom_kind() => _custom.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_refuse_a_command_without_a_handler() => _unknownCommand.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_refuse_a_query_without_a_performer() => _unknownQuery.ShouldBeOfExactType<McpExposedOperationUnknown>();
    [Fact] void should_map_a_known_command() => _control.ShouldBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
