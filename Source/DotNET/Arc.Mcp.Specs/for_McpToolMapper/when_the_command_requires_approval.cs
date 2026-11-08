// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Mcp.for_McpToolMapper.given;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_the_command_requires_approval : a_mapper_context
{
    Exception? _direct;
    Exception? _inherited;
    Exception? _control;
    McpToolDescriptor? _mapped;

    void Because()
    {
        _direct = Catch.Exception(() => MapCommand<ApprovalRequiredCommand>("direct"));
        _inherited = Catch.Exception(() => MapCommand<DerivedFromApprovalRequiredCommand>("inherited"));
        _control = Catch.Exception(() => _mapped = MapCommand<DeclaredCommand>("control"));
    }

    [Fact] void should_refuse_the_marked_command() => _direct.ShouldBeOfExactType<McpApprovalRequiredCommandExposed>();
    [Fact] void should_refuse_a_command_inheriting_the_requirement() => _inherited.ShouldBeOfExactType<McpApprovalRequiredCommandExposed>();
    [Fact] void should_map_a_command_without_the_requirement() => (_control is null && _mapped is not null).ShouldBeTrue();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
