// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Mcp.for_McpToolMapper.given;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_an_operation_declares_a_blank_scope : a_mapper_context
{
    Exception? _blank;
    Exception? _control;

    void Establish() => AddHandlerFor<BlankScopeCommand>();

    void Because()
    {
        _blank = Catch.Exception(() => MapCommand<BlankScopeCommand>("blank_scope"));
        _control = Catch.Exception(() => MapCommand<DeclaredCommand>("control"));
    }

    [Fact] void should_refuse_the_blank_scope() => _blank.ShouldBeOfExactType<McpToolScopeBlank>();
    [Fact] void should_accept_a_command_without_scopes() => _control.ShouldBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
